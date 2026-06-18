using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mosaic.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldLocaleContentPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_role_content_type_permissions_RoleId_ContentTypeApiName",
                schema: "identity",
                table: "role_content_type_permissions");

            migrationBuilder.DropIndex(
                name: "IX_content_type_permissions_UserId_ContentTypeApiName",
                schema: "identity",
                table: "content_type_permissions");

            migrationBuilder.AddColumn<string>(
                name: "FieldApiName",
                schema: "identity",
                table: "role_content_type_permissions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "identity",
                table: "role_content_type_permissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldApiName",
                schema: "identity",
                table: "content_type_permissions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                schema: "identity",
                table: "content_type_permissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_content_type_permissions_RoleId_ContentTypeApiName_Fie~",
                schema: "identity",
                table: "role_content_type_permissions",
                columns: new[] { "RoleId", "ContentTypeApiName", "FieldApiName", "Locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_type_permissions_UserId_ContentTypeApiName_FieldApi~",
                schema: "identity",
                table: "content_type_permissions",
                columns: new[] { "UserId", "ContentTypeApiName", "FieldApiName", "Locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_role_content_type_permissions_RoleId_ContentTypeApiName_Fie~",
                schema: "identity",
                table: "role_content_type_permissions");

            migrationBuilder.DropIndex(
                name: "IX_content_type_permissions_UserId_ContentTypeApiName_FieldApi~",
                schema: "identity",
                table: "content_type_permissions");

            migrationBuilder.DropColumn(
                name: "FieldApiName",
                schema: "identity",
                table: "role_content_type_permissions");

            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "identity",
                table: "role_content_type_permissions");

            migrationBuilder.DropColumn(
                name: "FieldApiName",
                schema: "identity",
                table: "content_type_permissions");

            migrationBuilder.DropColumn(
                name: "Locale",
                schema: "identity",
                table: "content_type_permissions");

            migrationBuilder.CreateIndex(
                name: "IX_role_content_type_permissions_RoleId_ContentTypeApiName",
                schema: "identity",
                table: "role_content_type_permissions",
                columns: new[] { "RoleId", "ContentTypeApiName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_type_permissions_UserId_ContentTypeApiName",
                schema: "identity",
                table: "content_type_permissions",
                columns: new[] { "UserId", "ContentTypeApiName" },
                unique: true);
        }
    }
}
