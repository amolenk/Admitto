using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillRegistrationAttendeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The previous migration added first_name/last_name/status as NOT NULL with an
            // empty-string default, which the domain value objects reject on materialization.
            // Backfill any rows that were created before the new columns existed so that
            // EF can rehydrate them.
            migrationBuilder.Sql(
                "UPDATE registrations.registrations SET first_name = 'Unknown' WHERE first_name = '';");
            migrationBuilder.Sql(
                "UPDATE registrations.registrations SET last_name = 'Unknown' WHERE last_name = '';");
            migrationBuilder.Sql(
                "UPDATE registrations.registrations SET status = 'Registered' WHERE status = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill: nothing to revert.
        }
    }
}
