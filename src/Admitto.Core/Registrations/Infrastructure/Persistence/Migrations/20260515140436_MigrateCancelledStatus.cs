using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateCancelledStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate Cancelled (1) → Archived (2) in ticketed_events.
            migrationBuilder.Sql("""
                UPDATE registrations.ticketed_events SET status = 2 WHERE status = 1;
                """);

            // Migrate Cancelled (1) → Archived (2) in ticket_catalog.event_status.
            migrationBuilder.Sql("""
                UPDATE registrations.ticket_catalog SET event_status = 2 WHERE event_status = 1;
                """);

            // Remove the obsolete is_cancelled property from ticket_types JSON.
            migrationBuilder.Sql("""
                UPDATE registrations.ticket_catalog
                SET ticket_types = (
                    SELECT jsonb_agg(tt - 'is_cancelled')
                    FROM jsonb_array_elements(ticket_types) AS tt
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
