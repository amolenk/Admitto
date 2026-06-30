using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "registrations");

            migrationBuilder.CreateTable(
                name: "activity_log_view",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_type = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_log_view", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    allowed_ticket_type_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    bypass_registration_window = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "otp_codes",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    superseded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "registrations",
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
                name: "processed_messages",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registrations",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    has_reconfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    reconfirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    additional_details = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    tickets = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ticket_catalog",
                schema: "registrations",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ticket_types = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_catalog", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "ticketed_events",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    website_url = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    base_url = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    public_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValueSql: "'event-' || substr(md5(random()::text), 1, 12)"),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "UTC"),
                    status = table.Column<int>(type: "integer", nullable: false),
                    registration_policy_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    registration_policy_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    registration_policy_allowed_email_domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    reconfirm_policy_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconfirm_policy_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconfirm_policy_cadence = table.Column<TimeSpan>(type: "interval", nullable: true),
                    reconfirm_policy_min_email_interval = table.Column<TimeSpan>(type: "interval", nullable: true),
                    waitlist_policy_quiet_hours_start = table.Column<TimeOnly>(type: "time", nullable: false, defaultValue: new TimeOnly(22, 0, 0)),
                    waitlist_policy_quiet_hours_end = table.Column<TimeOnly>(type: "time", nullable: false, defaultValue: new TimeOnly(8, 0, 0)),
                    additional_detail_schema = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticketed_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "waitlists",
                schema: "registrations",
                columns: table => new
                {
                    ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    entries = table.Column<string>(type: "jsonb", nullable: true),
                    waitlist_coupons = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitlists", x => x.ticket_type_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_view_registration_type_occurred",
                schema: "registrations",
                table: "activity_log_view",
                columns: new[] { "team_id", "event_id", "registration_id", "activity_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_coupons_code",
                schema: "registrations",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_event_id_team_id",
                schema: "registrations",
                table: "coupons",
                columns: new[] { "event_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "IX_otp_codes_email_hash_event_id",
                schema: "registrations",
                table: "otp_codes",
                columns: new[] { "email_hash", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_pending_created_at",
                schema: "registrations",
                table: "outbox",
                columns: new[] { "state", "created_at" },
                filter: "state = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state",
                schema: "registrations",
                table: "outbox",
                column: "state",
                filter: "state = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_processed_messages_message_key",
                schema: "registrations",
                table: "processed_messages",
                column: "message_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_registrations_event_id_email",
                schema: "registrations",
                table: "registrations",
                columns: new[] { "event_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_public_slug",
                schema: "registrations",
                table: "ticketed_events",
                column: "public_slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_team_id_status",
                schema: "registrations",
                table: "ticketed_events",
                columns: new[] { "team_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_waitlists_event_id",
                schema: "registrations",
                table: "waitlists",
                column: "event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_log_view",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "coupons",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "otp_codes",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "processed_messages",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "registrations",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "ticket_catalog",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "ticketed_events",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "waitlists",
                schema: "registrations");
        }
    }
}
