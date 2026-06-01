using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMSDashboard.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContentItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Track = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                BetaUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ProdUploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                StatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ContentItems", x => x.Id));

        migrationBuilder.CreateTable(
            name: "JobRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JobType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Result = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_JobRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ReportCaches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReportKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ReportCaches", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SyncLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SheetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                RowsUpdated = table.Column<int>(type: "int", nullable: false),
                SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SyncLogs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ValidationLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RuleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ValidationLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ValidationLogs_ContentItems_ContentItemId",
                    column: x => x.ContentItemId,
                    principalTable: "ContentItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Indexes
        migrationBuilder.CreateIndex("IX_ContentItems_CreatedAt", "ContentItems", "CreatedAt");
        migrationBuilder.CreateIndex("IX_ContentItems_IsDeleted", "ContentItems", "IsDeleted");
        migrationBuilder.CreateIndex("IX_ContentItems_Status", "ContentItems", "Status");
        migrationBuilder.CreateIndex("IX_ContentItems_StatusChangedAt", "ContentItems", "StatusChangedAt");
        migrationBuilder.CreateIndex("IX_ContentItems_Track", "ContentItems", "Track");
        migrationBuilder.CreateIndex("IX_ContentItems_Type", "ContentItems", "Type");
        migrationBuilder.CreateIndex("IX_ReportCaches_ReportKey", "ReportCaches", "ReportKey", unique: true);
        migrationBuilder.CreateIndex("IX_SyncLogs_SyncedAt", "SyncLogs", "SyncedAt");
        migrationBuilder.CreateIndex("IX_ValidationLogs_ContentItemId", "ValidationLogs", "ContentItemId");

        InsertSeedData(migrationBuilder);
    }

    private static void InsertSeedData(MigrationBuilder migrationBuilder)
    {
        var tracks = new[] { "Foundation", "B1", "Advanced", "Applied", "Crescent" };
        var types = new[] { "Quiz", "Reading", "Audio", "PPT", "Activity" };
        var difficulties = new[] { "Easy", "Medium", "Hard" };
        var statuses = new[] { "Pending", "InBeta", "Validated", "InProduction", "Failed" };
        var users = new[] { "alice@nxtwave.in", "bob@nxtwave.in", "carol@nxtwave.in" };
        var baseDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 1; i <= 20; i++)
        {
            var status = statuses[i % statuses.Length];
            var createdAt = baseDate.AddDays(i - 1).AddHours(i);
            var betaAt = (status == "InBeta" || status == "Validated" || status == "InProduction")
                ? (DateTime?)createdAt.AddDays(1) : null;
            var validAt = (status == "Validated" || status == "InProduction")
                ? (DateTime?)createdAt.AddDays(2) : null;
            var prodAt = status == "InProduction" ? (DateTime?)createdAt.AddDays(3) : null;

            migrationBuilder.InsertData(
                table: "ContentItems",
                columns: new[]
                {
                    "Id","Title","Type","Track","Difficulty","Status",
                    "BetaUploadedAt","ProdUploadedAt","ValidatedAt",
                    "CreatedAt","StatusChangedAt","CreatedBy","Notes","IsDeleted"
                },
                values: new object[]
                {
                    Guid.Parse($"00000000-0000-0000-0000-{i:D12}"),
                    $"Sample Content Item {i}",
                    types[i % types.Length],
                    tracks[i % tracks.Length],
                    difficulties[i % difficulties.Length],
                    status,
                    betaAt,
                    prodAt,
                    validAt,
                    createdAt,
                    createdAt,
                    users[i % users.Length],
                    $"Seeded item {i} for demo",
                    false
                });
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ValidationLogs");
        migrationBuilder.DropTable(name: "ContentItems");
        migrationBuilder.DropTable(name: "JobRecords");
        migrationBuilder.DropTable(name: "ReportCaches");
        migrationBuilder.DropTable(name: "SyncLogs");
    }
}
