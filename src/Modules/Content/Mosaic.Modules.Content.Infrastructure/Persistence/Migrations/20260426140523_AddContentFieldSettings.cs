using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mosaic.Modules.Content.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentFieldSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                schema: "content",
                table: "content_fields",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeprecated",
                schema: "content",
                table: "content_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRepeatable",
                schema: "content",
                table: "content_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnique",
                schema: "content",
                table: "content_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxLength",
                schema: "content",
                table: "content_fields",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxNumber",
                schema: "content",
                table: "content_fields",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinLength",
                schema: "content",
                table: "content_fields",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinNumber",
                schema: "content",
                table: "content_fields",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegexPattern",
                schema: "content",
                table: "content_fields",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationTargetContentTypeApiName",
                schema: "content",
                table: "content_fields",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredLocales",
                schema: "content",
                table: "content_fields",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultValue",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "IsDeprecated",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "IsRepeatable",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "IsUnique",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "MaxLength",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "MaxNumber",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "MinLength",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "MinNumber",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "RegexPattern",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "RelationTargetContentTypeApiName",
                schema: "content",
                table: "content_fields");

            migrationBuilder.DropColumn(
                name: "RequiredLocales",
                schema: "content",
                table: "content_fields");
        }
    }
}
