using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mosaic.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceAccountsAndTokenMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsServiceAccount",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                schema: "identity",
                table: "access_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "UserLogin");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "identity",
                table: "access_tokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsServiceAccount",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Kind",
                schema: "identity",
                table: "access_tokens");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "identity",
                table: "access_tokens");
        }
    }
}
