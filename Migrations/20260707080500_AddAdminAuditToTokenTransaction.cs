using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditToTokenTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "TokenTransactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdminUserId",
                table: "TokenTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_AdminUserId",
                table: "TokenTransactions",
                column: "AdminUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TokenTransactions_AdminUserId",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "TokenTransactions");

            migrationBuilder.DropColumn(
                name: "AdminUserId",
                table: "TokenTransactions");
        }
    }
}
