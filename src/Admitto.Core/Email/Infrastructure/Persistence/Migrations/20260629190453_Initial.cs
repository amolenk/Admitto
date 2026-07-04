using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "email");

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

            migrationBuilder.CreateTable(
                name: "event_email_context_view",
                schema: "email",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    public_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reconfirm_opens_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reconfirm_closes_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    reconfirm_cadence_hours = table.Column<int>(type: "integer", nullable: true),
                    reconfirm_min_email_interval_hours = table.Column<int>(type: "integer", nullable: true),
                    self_service_ticket_type_count = table.Column<int>(type: "integer", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    ticketed_event_version = table.Column<long>(type: "bigint", nullable: false),
                    ticket_catalog_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_email_context_view", x => new { x.team_id, x.ticketed_event_id });
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "team_email_context_view",
                schema: "email",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    accent_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    reply_to_email_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    team_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_email_context_view", x => x.team_id);
                });

            migrationBuilder.CreateTable(
                name: "email_log",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    email_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    status_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    delivery_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    bulk_email_job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_log_bulk_email_jobs_bulk_email_job_id",
                        column: x => x.bulk_email_job_id,
                        principalSchema: "email",
                        principalTable: "bulk_email_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_email_log_bulk_email_job_id",
                schema: "email",
                table: "email_log",
                column: "bulk_email_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_recipient_idempotency",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "recipient", "idempotency_key" },
                unique: true,
                filter: "ticketed_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_registration",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "registration_id" });

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_sent_at",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_email_log_system_recipient_idempotency",
                schema: "email",
                table: "email_log",
                columns: new[] { "recipient", "idempotency_key" },
                unique: true,
                filter: "ticketed_event_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_event_email_context_view_reconfirm_schedule",
                schema: "email",
                table: "event_email_context_view",
                columns: new[] { "is_archived", "reconfirm_opens_at", "reconfirm_closes_at" });

            migrationBuilder.CreateIndex(
                name: "IX_event_email_context_view_team_id",
                schema: "email",
                table: "event_email_context_view",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_pending_created_at",
                schema: "email",
                table: "outbox",
                columns: new[] { "state", "created_at" },
                filter: "state = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state",
                schema: "email",
                table: "outbox",
                column: "state",
                filter: "state = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_log",
                schema: "email");

            migrationBuilder.DropTable(
                name: "event_email_context_view",
                schema: "email");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "email");

            migrationBuilder.DropTable(
                name: "team_email_context_view",
                schema: "email");

            migrationBuilder.DropTable(
                name: "bulk_email_jobs",
                schema: "email");
        }
    }
}
