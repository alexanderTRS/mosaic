using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mosaic.Modules.Search.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSearchSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.CreateTable(
                name: "content_item_documents",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeApiName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_item_documents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_item_documents_ContentItemId",
                schema: "search",
                table: "content_item_documents",
                column: "ContentItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_item_documents_ContentTypeApiName",
                schema: "search",
                table: "content_item_documents",
                column: "ContentTypeApiName");

            migrationBuilder.CreateIndex(
                name: "IX_content_item_documents_Status",
                schema: "search",
                table: "content_item_documents",
                column: "Status");

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_content_item_documents_SearchText_tsv"
                ON search.content_item_documents
                USING GIN (to_tsvector('simple', "SearchText"));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS search."IX_content_item_documents_SearchText_tsv";
                """);

            migrationBuilder.DropTable(
                name: "content_item_documents",
                schema: "search");
        }
    }
}
