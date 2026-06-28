using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveWaitlistQuietHoursToPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "quiet_hours_start",
                schema: "registrations",
                table: "ticketed_events",
                newName: "waitlist_policy_quiet_hours_start");

            migrationBuilder.RenameColumn(
                name: "quiet_hours_end",
                schema: "registrations",
                table: "ticketed_events",
                newName: "waitlist_policy_quiet_hours_end");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "waitlist_policy_quiet_hours_start",
                schema: "registrations",
                table: "ticketed_events",
                newName: "quiet_hours_start");

            migrationBuilder.RenameColumn(
                name: "waitlist_policy_quiet_hours_end",
                schema: "registrations",
                table: "ticketed_events",
                newName: "quiet_hours_end");
        }
    }
}
