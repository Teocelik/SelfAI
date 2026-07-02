using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SelfAI.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetsAndMigrateCharacterFaceReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FaceReferenceUrls",
                table: "Characters",
                newName: "FaceReferenceAssetIds");

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_DeletedAt",
                table: "Assets",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserId",
                table: "Assets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserId_Purpose",
                table: "Assets",
                columns: new[] { "UserId", "Purpose" });

            // ═══ F.M.7 DATA MIGRATION ═══
            // FaceReferenceUrls → FaceReferenceAssetIds rename ile kolon aynı kaldı ama
            // içerik hâlâ URL JSON'u (["url1","url2"]). Her URL için Asset kaydı oluştur,
            // kolonu Asset ID JSON'u (["guid","guid"]) ile değiştir. OPENJSON: SQL Server 2016+
            // (bu DB SQL Server 2019, compat 150 — desteklenir).
            migrationBuilder.Sql(@"
    DECLARE @characterId UNIQUEIDENTIFIER;
    DECLARE @userId UNIQUEIDENTIFIER;
    DECLARE @urlsJson NVARCHAR(MAX);

    DECLARE char_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, UserId, FaceReferenceAssetIds
        FROM Characters
        WHERE FaceReferenceAssetIds IS NOT NULL
          AND FaceReferenceAssetIds NOT IN ('[]', '')
          AND ISJSON(FaceReferenceAssetIds) = 1
          AND FaceReferenceAssetIds LIKE '%http%';  -- yalnızca URL içerenler (guid'e dönüşmemiş)

    OPEN char_cursor;
    FETCH NEXT FROM char_cursor INTO @characterId, @userId, @urlsJson;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @assetIds NVARCHAR(MAX) = '[';
        DECLARE @first BIT = 1;

        DECLARE @url NVARCHAR(2000);
        DECLARE url_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT value FROM OPENJSON(@urlsJson);

        OPEN url_cursor;
        FETCH NEXT FROM url_cursor INTO @url;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @assetId UNIQUEIDENTIFIER = NEWID();

            INSERT INTO Assets
                (Id, UserId, StorageProvider, Url, StorageKey, Purpose,
                 ContentType, SizeBytes, OriginalFileName, CreatedAt, DeletedAt)
            VALUES
                (@assetId, @userId, 'FalAi', @url, NULL, 0,  -- Purpose 0 = CharacterTraining
                 'image/jpeg', 0, NULL, GETUTCDATE(), NULL);

            IF @first = 1
                SET @assetIds = @assetIds + '""' + CAST(@assetId AS NVARCHAR(36)) + '""';
            ELSE
                SET @assetIds = @assetIds + ',""' + CAST(@assetId AS NVARCHAR(36)) + '""';
            SET @first = 0;

            FETCH NEXT FROM url_cursor INTO @url;
        END;

        CLOSE url_cursor;
        DEALLOCATE url_cursor;

        SET @assetIds = @assetIds + ']';

        UPDATE Characters
        SET FaceReferenceAssetIds = @assetIds
        WHERE Id = @characterId;

        FETCH NEXT FROM char_cursor INTO @characterId, @userId, @urlsJson;
    END;

    CLOSE char_cursor;
    DEALLOCATE char_cursor;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.RenameColumn(
                name: "FaceReferenceAssetIds",
                table: "Characters",
                newName: "FaceReferenceUrls");
        }
    }
}
