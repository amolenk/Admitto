using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistQueuedCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshot-only: TicketType.WaitlistQueuedCount lives in the ticket_types JSON column, and ticket types
            // stored without the "waitlist_queued_count" key read back as 0, so no schema change or backfill.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
