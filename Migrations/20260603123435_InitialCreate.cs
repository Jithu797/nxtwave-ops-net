using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LMSDashboard.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Track = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRecords", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportCaches", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

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

            migrationBuilder.InsertData(
                table: "ContentItems",
                columns: new[] { "Id", "BetaUploadedAt", "CreatedAt", "CreatedBy", "Difficulty", "IsDeleted", "Notes", "ProdUploadedAt", "Status", "StatusChangedAt", "Title", "Track", "Type", "ValidatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 5, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 1, 1, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 1 for demo", null, "InBeta", new DateTime(2026, 5, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 1", "B1", "Reading", null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2026, 5, 3, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 2, 2, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 2 for demo", null, "Validated", new DateTime(2026, 5, 2, 2, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 2", "Advanced", "Audio", new DateTime(2026, 5, 4, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2026, 5, 4, 3, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 3, 3, 0, 0, 0, DateTimeKind.Utc), "alice@nxtwave.in", "Easy", false, "Seeded item 3 for demo", new DateTime(2026, 5, 6, 3, 0, 0, 0, DateTimeKind.Utc), "InProduction", new DateTime(2026, 5, 3, 3, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 3", "Applied", "PPT", new DateTime(2026, 5, 5, 3, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2026, 5, 5, 4, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 4, 4, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 4 for demo", null, "Failed", new DateTime(2026, 5, 4, 4, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 4", "Crescent", "Activity", new DateTime(2026, 5, 6, 4, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000005"), null, new DateTime(2026, 5, 5, 5, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 5 for demo", null, "Pending", new DateTime(2026, 5, 5, 5, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 5", "Foundation", "Quiz", null },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new DateTime(2026, 5, 7, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 6, 6, 0, 0, 0, DateTimeKind.Utc), "alice@nxtwave.in", "Easy", false, "Seeded item 6 for demo", null, "InBeta", new DateTime(2026, 5, 6, 6, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 6", "B1", "Reading", null },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new DateTime(2026, 5, 8, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 7, 7, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 7 for demo", null, "Validated", new DateTime(2026, 5, 7, 7, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 7", "Advanced", "Audio", new DateTime(2026, 5, 9, 7, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2026, 5, 9, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 8, 8, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 8 for demo", new DateTime(2026, 5, 11, 8, 0, 0, 0, DateTimeKind.Utc), "InProduction", new DateTime(2026, 5, 8, 8, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 8", "Applied", "PPT", new DateTime(2026, 5, 10, 8, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new DateTime(2026, 5, 10, 9, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 9, 9, 0, 0, 0, DateTimeKind.Utc), "alice@nxtwave.in", "Easy", false, "Seeded item 9 for demo", null, "Failed", new DateTime(2026, 5, 9, 9, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 9", "Crescent", "Activity", new DateTime(2026, 5, 11, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000010"), null, new DateTime(2026, 5, 10, 10, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 10 for demo", null, "Pending", new DateTime(2026, 5, 10, 10, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 10", "Foundation", "Quiz", null },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2026, 5, 12, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 11, 11, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 11 for demo", null, "InBeta", new DateTime(2026, 5, 11, 11, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 11", "B1", "Reading", null },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2026, 5, 13, 12, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 12, 12, 0, 0, 0, DateTimeKind.Utc), "alice@nxtwave.in", "Easy", false, "Seeded item 12 for demo", null, "Validated", new DateTime(2026, 5, 12, 12, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 12", "Advanced", "Audio", new DateTime(2026, 5, 14, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new DateTime(2026, 5, 14, 13, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 13, 13, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 13 for demo", new DateTime(2026, 5, 16, 13, 0, 0, 0, DateTimeKind.Utc), "InProduction", new DateTime(2026, 5, 13, 13, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 13", "Applied", "PPT", new DateTime(2026, 5, 15, 13, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new DateTime(2026, 5, 15, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 14, 14, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 14 for demo", null, "Failed", new DateTime(2026, 5, 14, 14, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 14", "Crescent", "Activity", new DateTime(2026, 5, 16, 14, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000015"), null, new DateTime(2026, 5, 15, 15, 0, 0, 0, DateTimeKind.Utc), "alice@nxtwave.in", "Easy", false, "Seeded item 15 for demo", null, "Pending", new DateTime(2026, 5, 15, 15, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 15", "Foundation", "Quiz", null },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new DateTime(2026, 5, 17, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 16, 16, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 16 for demo", null, "InBeta", new DateTime(2026, 5, 16, 16, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 16", "B1", "Reading", null },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new DateTime(2026, 5, 18, 17, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 17, 17, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 17 for demo", null, "Validated", new DateTime(2026, 5, 17, 17, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 17", "Advanced", "Audio", new DateTime(2026, 5, 19, 17, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new DateTime(2026, 5, 19, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 18, 18, 0, 0, 0, DateTimeKind.Utc), "alice@nxtwave.in", "Easy", false, "Seeded item 18 for demo", new DateTime(2026, 5, 21, 18, 0, 0, 0, DateTimeKind.Utc), "InProduction", new DateTime(2026, 5, 18, 18, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 18", "Applied", "PPT", new DateTime(2026, 5, 20, 18, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new DateTime(2026, 5, 20, 19, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 19, 19, 0, 0, 0, DateTimeKind.Utc), "bob@nxtwave.in", "Medium", false, "Seeded item 19 for demo", null, "Failed", new DateTime(2026, 5, 19, 19, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 19", "Crescent", "Activity", new DateTime(2026, 5, 21, 19, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000020"), null, new DateTime(2026, 5, 20, 20, 0, 0, 0, DateTimeKind.Utc), "carol@nxtwave.in", "Hard", false, "Seeded item 20 for demo", null, "Pending", new DateTime(2026, 5, 20, 20, 0, 0, 0, DateTimeKind.Utc), "Sample Content Item 20", "Foundation", "Quiz", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_CreatedAt",
                table: "ContentItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_IsDeleted",
                table: "ContentItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Status",
                table: "ContentItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_StatusChangedAt",
                table: "ContentItems",
                column: "StatusChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Track",
                table: "ContentItems",
                column: "Track");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_Type",
                table: "ContentItems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ReportCaches_ReportKey",
                table: "ReportCaches",
                column: "ReportKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_SyncedAt",
                table: "SyncLogs",
                column: "SyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationLogs_ContentItemId",
                table: "ValidationLogs",
                column: "ContentItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobRecords");

            migrationBuilder.DropTable(
                name: "ReportCaches");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "ValidationLogs");

            migrationBuilder.DropTable(
                name: "ContentItems");
        }
    }
}
