using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadModerationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadModerationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    NsfwScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadModerationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadModerationLogs_AppUserId_CreatedAt",
                table: "UploadModerationLogs",
                columns: new[] { "AppUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadModerationLogs");
        }
    }
}
