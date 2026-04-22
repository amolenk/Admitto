using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketedEventAndEventStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cancellation_policy",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "event_lifecycle_guard",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "event_registration_policy",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "reconfirm_policy",
                schema: "registrations");

            migrationBuilder.AddColumn<int>(
                name: "event_status",
                schema: "registrations",
                table: "ticket_catalog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ticketed_events",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    registration_policy_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    registration_policy_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    registration_policy_allowed_email_domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    cancellation_policy_late_cutoff = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconfirm_policy_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconfirm_policy_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconfirm_policy_cadence = table.Column<TimeSpan>(type: "interval", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticketed_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_team_id_slug",
                schema: "registrations",
                table: "ticketed_events",
                columns: new[] { "team_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticketed_events",
                schema: "registrations");

            migrationBuilder.DropColumn(
                name: "event_status",
                schema: "registrations",
                table: "ticket_catalog");

            migrationBuilder.CreateTable(
                name: "cancellation_policy",
                schema: "registrations",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    late_cancellation_cutoff = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancellation_policy", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "event_lifecycle_guard",
                schema: "registrations",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Active"),
                    policy_mutation_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_lifecycle_guard", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "event_registration_policy",
                schema: "registrations",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_email_domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    registration_window_closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    registration_window_opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_registration_policy", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "reconfirm_policy",
                schema: "registrations",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cadence = table.Column<TimeSpan>(type: "interval", nullable: false),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconfirm_policy", x => x.event_id);
                });
        }
    }
}
