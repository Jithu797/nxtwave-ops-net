using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LMSDashboard.Data;
using LMSDashboard.DTOs;
using LMSDashboard.Jobs;
using LMSDashboard.Middleware;
using LMSDashboard.Services;
using LMSDashboard.Validators;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Hangfire ──────────────────────────────────────────────────────────────────
var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConn, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<ISheetsService, SheetsService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAiStructureService, AiStructureService>();
builder.Services.AddScoped<IJobRecordService, JobRecordService>();
builder.Services.AddScoped<SheetsSyncJob>();
builder.Services.AddScoped<NightlyReportJob>();

// ── HTTP Client ───────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("AiService", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required in configuration.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// ── Razor Pages + Controllers ─────────────────────────────────────────────────
builder.Services.AddRazorPages(opts =>
{
    // All Razor Pages default to allowing anonymous access (internal tool)
    // Individual pages can add [Authorize] if needed
});
builder.Services.AddControllers();
builder.Services.AddAntiforgery();

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateStatusRequestValidator>();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LMS Content Operations Dashboard API",
        Version = "v1",
        Description = "Internal B2B API for NxtWave content operations team."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Auto-migrate + seed on startup ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LMS Dashboard v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
    app.UseStatusCodePagesWithReExecute("/404");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Hangfire Dashboard (accessible in dev without auth) ───────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = app.Environment.IsDevelopment()
        ? new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
        : Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
});

// ── Hangfire Recurring Jobs ───────────────────────────────────────────────────
RecurringJob.AddOrUpdate<SheetsSyncJob>(
    "sync-pending-status-changes",
    job => job.SyncPendingStatusChanges(),
    "*/15 * * * *");

RecurringJob.AddOrUpdate<NightlyReportJob>(
    "nightly-report",
    job => job.GenerateNightlyReport(),
    "0 2 * * *");

// ── Routing ───────────────────────────────────────────────────────────────────
app.MapRazorPages();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/Dashboard"));

app.Run();
