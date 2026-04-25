using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkEmailJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "bulk_email_job_id",
                schema: "email",
                table: "email_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bulk_email_jobs",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    text_body = table.Column<string>(type: "text", nullable: true),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "jsonb", nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    is_system_triggered = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recipient_count = table.Column<int>(type: "integer", nullable: false),
                    sent_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    cancelled_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancellation_requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    recipients = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bulk_email_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_log_bulk_email_job_id",
                schema: "email",
                table: "email_log",
                column: "bulk_email_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_bulk_email_jobs_event_created_at",
                schema: "email",
                table: "bulk_email_jobs",
                columns: new[] { "ticketed_event_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_bulk_email_jobs_status",
                schema: "email",
                table: "bulk_email_jobs",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_email_log_bulk_email_jobs_bulk_email_job_id",
                schema: "email",
                table: "email_log",
                column: "bulk_email_job_id",
                principalSchema: "email",
                principalTable: "bulk_email_jobs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_log_bulk_email_jobs_bulk_email_job_id",
                schema: "email",
                table: "email_log");

            migrationBuilder.DropTable(
                name: "bulk_email_jobs",
                schema: "email");

            migrationBuilder.DropIndex(
                name: "IX_email_log_bulk_email_job_id",
                schema: "email",
                table: "email_log");

            migrationBuilder.DropColumn(
                name: "bulk_email_job_id",
                schema: "email",
                table: "email_log");
        }
    }
}
