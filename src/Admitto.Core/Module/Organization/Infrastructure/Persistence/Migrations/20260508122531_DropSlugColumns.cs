using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Module.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropSlugColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_teams_slug",
                schema: "organization",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "slug",
                schema: "organization",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "requested_slug",
                schema: "organization",
                table: "team_event_creation_requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                schema: "organization",
                table: "teams",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "requested_slug",
                schema: "organization",
                table: "team_event_creation_requests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_teams_slug",
                schema: "organization",
                table: "teams",
                column: "slug",
                unique: true);
        }
    }
}
