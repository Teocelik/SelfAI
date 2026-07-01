using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFluxControlNetSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F.M.6 hotfix — Pose Lock (ControlNet) UI'dan kaldırıldı: fal.ai'da uygun native
            // OpenPose endpoint yok. Catalog satırı siliniyor; backend generator kodu ileride
            // için duruyor. Down() satırı geri ekler (Status=1/Pending — kullanıcıya gizli).
            migrationBuilder.Sql(@"
                DELETE FROM ModelCatalogEntries
                WHERE EndpointId = 'fal-ai/flux-controlnet';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM ModelCatalogEntries WHERE EndpointId = 'fal-ai/flux-controlnet')
                    INSERT INTO ModelCatalogEntries
                    (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt)
                    VALUES
                    (NEWID(), 'fal-ai/flux-controlnet', 'Flux ControlNet (Pose Transfer)',
                     'Referans görsel pozunu kullanarak yeni karakter üretir',
                     'text-to-image', 'fal.ai', 0.04, 'Standard', 1, 0, GETUTCDATE());
            ");
        }
    }
}
