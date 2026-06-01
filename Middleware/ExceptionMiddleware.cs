using System.Net;
using System.Text.Json;
using LMSDashboard.DTOs;

namespace LMSDashboard.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // API requests → always return JSON
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var detail = _env.IsDevelopment() ? ex.Message : "An internal server error occurred.";
            var response = ApiResponse<object>.Fail("An internal server error occurred.", new List<string> { detail });
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        else
        {
            // Razor Pages → redirect to Error page
            context.Response.Redirect("/Error");
        }
    }
}
