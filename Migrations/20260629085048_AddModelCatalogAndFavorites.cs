using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class AddModelCatalogAndFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelCatalogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CostUsd = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsRecommended = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelCatalogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserFavoriteModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavoriteModels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModelCatalogEntries_Category",
                table: "ModelCatalogEntries",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ModelCatalogEntries_EndpointId",
                table: "ModelCatalogEntries",
                column: "EndpointId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelCatalogEntries_Status",
                table: "ModelCatalogEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavoriteModels_UserId_EndpointId",
                table: "UserFavoriteModels",
                columns: new[] { "UserId", "EndpointId" },
                unique: true);

            // ═══ F.M.5 — Curated başlangıç catalog'u (9 Approved model) ═══
            // Status: 0=Approved. CostUsd değerleri fal.ai tahmini (admin panelinden güncellenebilir).
            migrationBuilder.Sql(@"
                INSERT INTO ModelCatalogEntries (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt) VALUES
                (NEWID(), 'fal-ai/flux/schnell', 'FLUX.1 [schnell]', 'Hızlı ve etkili text-to-image', 'text-to-image', 'Black Forest Labs', 0.003, 'Fast', 0, 1, GETUTCDATE()),
                (NEWID(), 'fal-ai/recraft-20b', 'Recraft 20B', 'Çok yönlü görsel üretim', 'text-to-image', 'Recraft', 0.004, 'Fast', 0, 0, GETUTCDATE()),
                (NEWID(), 'fal-ai/flux/dev', 'FLUX.1 [dev]', 'Yüksek kalite text-to-image', 'text-to-image', 'Black Forest Labs', 0.025, 'Standard', 0, 1, GETUTCDATE()),
                (NEWID(), 'fal-ai/seedream-3', 'Seedream 3.0', 'Detaylı sahne üretimi', 'text-to-image', 'ByteDance', 0.036, 'Standard', 0, 0, GETUTCDATE()),
                (NEWID(), 'fal-ai/imagen3', 'Google Imagen 3', 'Google fotorealistik model', 'text-to-image', 'Google', 0.05, 'Standard', 0, 0, GETUTCDATE()),
                (NEWID(), 'fal-ai/flux-pro/v1.1', 'FLUX.1.1 [pro]', 'Premium kalite Flux', 'text-to-image', 'Black Forest Labs', 0.06, 'Premium', 0, 1, GETUTCDATE()),
                (NEWID(), 'fal-ai/recraft-v3', 'Recraft V3', 'Premium çok yönlü', 'text-to-image', 'Recraft', 0.06, 'Premium', 0, 0, GETUTCDATE()),
                (NEWID(), 'fal-ai/nano-banana', 'Google Nano Banana', 'Hızlı Google modeli', 'text-to-image', 'Google', 0.07, 'Premium', 0, 0, GETUTCDATE()),
                (NEWID(), 'fal-ai/gpt-image-1', 'GPT Image', 'OpenAI text-rich image', 'text-to-image', 'OpenAI', 0.13, 'Premium', 0, 0, GETUTCDATE());
            ");

            // ═══ F.M.5 — Karakter LoRA inference cost lookup'ı (Hidden — kullanıcıya görünmez) ═══
            // Status: 2=Hidden. GenerationOrchestrator karakter yolunda bu kaydın CostUsd/Tier'ından
            // credit hesaplar; model picker'da listelenmez.
            migrationBuilder.Sql(@"
                INSERT INTO ModelCatalogEntries (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt) VALUES
                (NEWID(), 'fal-ai/flux-lora', 'Karakter LoRA', 'Eğitilmiş karakter LoRA inference (dahili)', 'text-to-image', 'Black Forest Labs', 0.025, 'CharacterLora', 2, 0, GETUTCDATE());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Seed kayıtları tablo drop ile birlikte gider; ayrı DELETE gerekmez.
            migrationBuilder.DropTable(
                name: "ModelCatalogEntries");

            migrationBuilder.DropTable(
                name: "UserFavoriteModels");
        }
    }
}
