using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class FixFluxPulidEndpointId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F.M.6 hotfix — Face Lock endpoint ID düzeltmesi: fal.ai'daki gerçek endpoint
            // "fal-ai/flux-pulid" ("fal-ai/pulid-flux" YOK). Orchestrator artık flux-pulid'e
            // route ediyor; catalog satırının EndpointId'si de eşleştiriliyor (yoksa cost/tier
            // lookup başarısız olurdu).
            migrationBuilder.Sql(@"
                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/flux-pulid',
                    UpdatedAt = GETUTCDATE()
                WHERE EndpointId = 'fal-ai/pulid-flux';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ModelCatalogEntries
                SET EndpointId = 'fal-ai/pulid-flux'
                WHERE EndpointId = 'fal-ai/flux-pulid';
            ");
        }
    }
}
