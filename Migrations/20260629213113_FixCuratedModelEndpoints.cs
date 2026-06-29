using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class FixCuratedModelEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F.M.5 hotfix-2: Curated seed'deki 4 yanlış endpoint ID'sini gerçek fal.ai
            // endpoint'leriyle düzelt. (Sync metadata güncelliyordu ama yanlış ID'ler
            // fal.ai'da eşleşmediğinden thumbnail null kalmıştı.)
            migrationBuilder.Sql(@"
                -- hotfix-1 testinde çalıştırılan /Catalog/Sync, doğru endpoint'leri Pending
                -- olarak eklemiş olabilir. Rename/INSERT collision'ını önlemek için hedef
                -- ID'lerdeki olası duplicate'leri önce temizle (taze DB'de no-op).
                DELETE FROM ModelCatalogEntries
                WHERE EndpointId IN (
                    'fal-ai/gpt-image-1.5',
                    'fal-ai/recraft/v3/text-to-image',
                    'fal-ai/bytedance/seedream/v4/text-to-image',
                    'fal-ai/ideogram/v3'
                );

                -- gpt-image-1 → gpt-image-1.5
                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/gpt-image-1.5',
                    DisplayName = 'GPT Image 1.5',
                    Description = 'OpenAI yüksek kaliteli görsel üretimi',
                    UpdatedAt = GETUTCDATE()
                WHERE EndpointId = 'fal-ai/gpt-image-1';

                -- recraft-v3 → recraft/v3/text-to-image
                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/recraft/v3/text-to-image',
                    UpdatedAt = GETUTCDATE()
                WHERE EndpointId = 'fal-ai/recraft-v3';

                -- seedream-3 → bytedance/seedream/v4/text-to-image
                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/bytedance/seedream/v4/text-to-image',
                    DisplayName = 'Seedream V4',
                    Description = 'ByteDance yeni nesil görsel üretim modeli (V4)',
                    CostUsd = 0.036,
                    UpdatedAt = GETUTCDATE()
                WHERE EndpointId = 'fal-ai/seedream-3';

                -- imagen3 fal.ai'da yok — sil
                DELETE FROM ModelCatalogEntries
                WHERE EndpointId = 'fal-ai/imagen3';

                -- Yerine Ideogram V3 ekle (Premium tier, tipografi/poster uzmanı)
                INSERT INTO ModelCatalogEntries
                (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt)
                VALUES
                (NEWID(), 'fal-ai/ideogram/v3', 'Ideogram V3',
                 'Tipografi ve poster uzmanı görsel modeli',
                 'text-to-image', 'Ideogram', 0.06, 'Premium', 0, 0, GETUTCDATE());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/gpt-image-1', DisplayName = 'GPT Image'
                WHERE EndpointId = 'fal-ai/gpt-image-1.5';

                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/recraft-v3'
                WHERE EndpointId = 'fal-ai/recraft/v3/text-to-image';

                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/seedream-3', DisplayName = 'Seedream 3.0',
                    Description = 'Detaylı sahne üretimi', CostUsd = 0.036
                WHERE EndpointId = 'fal-ai/bytedance/seedream/v4/text-to-image';

                DELETE FROM ModelCatalogEntries
                WHERE EndpointId = 'fal-ai/ideogram/v3';

                INSERT INTO ModelCatalogEntries
                (Id, EndpointId, DisplayName, Description, Category, Provider, CostUsd, Tier, Status, IsRecommended, CreatedAt)
                VALUES
                (NEWID(), 'fal-ai/imagen3', 'Google Imagen 3', 'Google fotorealistik model',
                 'text-to-image', 'Google', 0.05, 'Standard', 0, 0, GETUTCDATE());
            ");
        }
    }
}
