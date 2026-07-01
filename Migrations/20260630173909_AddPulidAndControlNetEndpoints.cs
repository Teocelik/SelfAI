using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class AddPulidAndControlNetEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F.M.6 — Face Lock (PuLID) ve Pose Lock (ControlNet) internal endpoint'leri.
            // Status = 1 (Pending) → /Catalog/Image (Approved filtresi) bunları kullanıcıya
            // GÖSTERMEZ; ama orchestrator cost/tier lookup'ı EndpointId ile bulur ve
            // isInternalPath sayesinde Approved guard'ından muaftır.
            // Tier string olarak saklanır (CatalogTierResolver çözer). NOT EXISTS guard'ı
            // tekrar/seed çakışmalarına karşı güvenlik sağlar.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM ModelCatalogEntries WHERE EndpointId = 'fal-ai/pulid-flux')
                    INSERT INTO ModelCatalogEntries
                    (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt)
                    VALUES
                    (NEWID(), 'fal-ai/pulid-flux', 'PuLID Flux (Face Transfer)',
                     'Tek yüz görseli ile karakter tutarlılığı sağlar',
                     'text-to-image', 'fal.ai', 0.06, 'Premium', 1, 0, GETUTCDATE());

                IF NOT EXISTS (SELECT 1 FROM ModelCatalogEntries WHERE EndpointId = 'fal-ai/flux-controlnet')
                    INSERT INTO ModelCatalogEntries
                    (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt)
                    VALUES
                    (NEWID(), 'fal-ai/flux-controlnet', 'Flux ControlNet (Pose Transfer)',
                     'Referans görsel pozunu kullanarak yeni karakter üretir',
                     'text-to-image', 'fal.ai', 0.04, 'Standard', 1, 0, GETUTCDATE());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ModelCatalogEntries
                WHERE EndpointId IN ('fal-ai/pulid-flux', 'fal-ai/flux-controlnet');
            ");
        }
    }
}
