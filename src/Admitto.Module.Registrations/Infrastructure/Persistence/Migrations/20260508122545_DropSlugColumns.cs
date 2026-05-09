using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropSlugColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ticketed_events_team_id_slug",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropColumn(
                name: "slug",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropColumn(
                name: "team_slug",
                schema: "registrations",
                table: "ticketed_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                schema: "registrations",
                table: "ticketed_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "team_slug",
                schema: "registrations",
                table: "ticketed_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_team_id_slug",
                schema: "registrations",
                table: "ticketed_events",
                columns: new[] { "team_id", "slug" },
                unique: true);
        }
    }
}
