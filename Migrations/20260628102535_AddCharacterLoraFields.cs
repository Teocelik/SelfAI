using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterLoraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AffogatoCharacterName",
                table: "Characters",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "AffogatoCharacterId",
                table: "Characters",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "FaceReferenceUrls",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoraModelUrl",
                table: "Characters",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoraTrainingJobId",
                table: "Characters",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoraTrainingStatus",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainingCompletedAt",
                table: "Characters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingFailureReason",
                table: "Characters",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainingStartedAt",
                table: "Characters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerWord",
                table: "Characters",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // F.M.4 — Mevcut Affogato karakterleri Migrated (CharacterStatus enum int = 3)
            // işaretle: modal'da gösterilmez. Status int saklanır (Active=1, Archived=2, Migrated=3).
            migrationBuilder.Sql(@"
                UPDATE Characters
                SET Status = 3
                WHERE Status = 1 AND AffogatoCharacterId IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceReferenceUrls",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "LoraModelUrl",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "LoraTrainingJobId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "LoraTrainingStatus",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TrainingCompletedAt",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TrainingFailureReason",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TrainingStartedAt",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "TriggerWord",
                table: "Characters");

            migrationBuilder.AlterColumn<string>(
                name: "AffogatoCharacterName",
                table: "Characters",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AffogatoCharacterId",
                table: "Characters",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
