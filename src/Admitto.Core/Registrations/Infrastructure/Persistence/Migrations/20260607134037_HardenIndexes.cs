using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_coupons_event_id",
                schema: "registrations",
                table: "coupons");

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_team_id_status",
                schema: "registrations",
                table: "ticketed_events",
                columns: new[] { "team_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state",
                schema: "registrations",
                table: "outbox",
                column: "state",
                filter: "state = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_coupons_event_id_team_id",
                schema: "registrations",
                table: "coupons",
                columns: new[] { "event_id", "team_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ticketed_events_team_id_status",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropIndex(
                name: "IX_outbox_state",
                schema: "registrations",
                table: "outbox");

            migrationBuilder.DropIndex(
                name: "IX_coupons_event_id_team_id",
                schema: "registrations",
                table: "coupons");

            migrationBuilder.CreateIndex(
                name: "IX_coupons_event_id",
                schema: "registrations",
                table: "coupons",
                column: "event_id");
        }
    }
}
