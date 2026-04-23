using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnifyEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create email_settings first so we can migrate data before dropping event_email_settings.
            migrationBuilder.CreateTable(
                name: "email_settings",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    smtp_port = table.Column<int>(type: "integer", nullable: false),
                    from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    auth_mode = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    protected_password = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_settings", x => x.id);
                });

            // Migrate data: copy all existing event_email_settings rows as (scope=Event=1, scope_id=ticketed_event_id).
            migrationBuilder.Sql(@"
                INSERT INTO email.email_settings (id, scope, scope_id, smtp_host, smtp_port, from_address, auth_mode, username, protected_password, created_at, last_changed_at, last_changed_by)
                SELECT gen_random_uuid(), 1, ticketed_event_id, smtp_host, smtp_port, from_address, auth_mode, username, protected_password, created_at, last_changed_at, last_changed_by
                FROM email.event_email_settings;
            ");

            migrationBuilder.DropTable(
                name: "event_email_settings",
                schema: "email");

            migrationBuilder.CreateTable(
                name: "email_log",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    email_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    status_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    text_body = table.Column<string>(type: "text", nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_recipient_idempotency",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "recipient", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_sent_at",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_scope_scope_id",
                schema: "email",
                table: "email_settings",
                columns: new[] { "scope", "scope_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates",
                columns: new[] { "scope", "scope_id", "type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_log",
                schema: "email");

            migrationBuilder.DropTable(
                name: "email_templates",
                schema: "email");

            migrationBuilder.CreateTable(
                name: "event_email_settings",
                schema: "email",
                columns: table => new
                {
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    from_address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    protected_password = table.Column<string>(type: "text", nullable: true),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    smtp_port = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_email_settings", x => x.ticketed_event_id);
                });

            // Restore data: copy event-scoped settings rows back to event_email_settings.
            migrationBuilder.Sql(@"
                INSERT INTO email.event_email_settings (ticketed_event_id, smtp_host, smtp_port, from_address, auth_mode, username, protected_password, created_at, last_changed_at, last_changed_by)
                SELECT scope_id, smtp_host, smtp_port, from_address, auth_mode, username, protected_password, created_at, last_changed_at, last_changed_by
                FROM email.email_settings WHERE scope = 1;
            ");

            migrationBuilder.DropTable(
                name: "email_settings",
                schema: "email");
        }
    }
}
