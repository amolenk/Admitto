using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

public partial class AddActiveReconfirmJobReservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            WITH ranked AS (
                SELECT id,
                       row_number() OVER (
                           PARTITION BY ticketed_event_id, email_type
                           ORDER BY created_at DESC, id DESC) AS position
                FROM email.bulk_email_jobs
                WHERE is_system_triggered = TRUE
                  AND email_type = 'Reconfirmation'
                  AND status IN ('Pending', 'Resolving', 'Sending')
            )
            UPDATE email.bulk_email_jobs AS jobs
            SET status = 'Failed',
                last_error = 'Superseded by the active reconfirm job reservation.',
                completed_at = now(),
                last_changed_at = now(),
                last_changed_by = 'reconfirm-reservation-migration'
            FROM ranked
            WHERE jobs.id = ranked.id
              AND ranked.position > 1;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_bulk_email_jobs_active_reconfirm_event",
            schema: "email",
            table: "bulk_email_jobs",
            columns: new[] { "ticketed_event_id", "email_type" },
            unique: true,
            filter: "is_system_triggered = TRUE AND email_type = 'Reconfirmation' AND status IN ('Pending', 'Resolving', 'Sending')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_bulk_email_jobs_active_reconfirm_event",
            schema: "email",
            table: "bulk_email_jobs");
    }
}
