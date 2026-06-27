using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketedEventPublicSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_slug",
                schema: "registrations",
                table: "ticketed_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValueSql: "'event-' || substr(md5(random()::text), 1, 12)");

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_public_slug",
                schema: "registrations",
                table: "ticketed_events",
                column: "public_slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ticketed_events_public_slug",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropColumn(
                name: "public_slug",
                schema: "registrations",
                table: "ticketed_events");
        }
    }
}
