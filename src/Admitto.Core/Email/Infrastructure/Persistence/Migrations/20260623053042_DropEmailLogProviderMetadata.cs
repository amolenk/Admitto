using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropEmailLogProviderMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "provider",
                schema: "email",
                table: "email_log");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                schema: "email",
                table: "email_log");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider",
                schema: "email",
                table: "email_log",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                schema: "email",
                table: "email_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
