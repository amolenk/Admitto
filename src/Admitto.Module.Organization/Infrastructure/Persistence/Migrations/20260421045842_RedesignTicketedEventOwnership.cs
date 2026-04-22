using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignTicketedEventOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticketed_events",
                schema: "organization");

            migrationBuilder.DropColumn(
                name: "ticketed_event_scope_version",
                schema: "organization",
                table: "teams");

            migrationBuilder.AddColumn<int>(
                name: "active_event_count",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "archived_event_count",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cancelled_event_count",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "pending_event_count",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "team_event_creation_requests",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    observed_event_status = table.Column<int>(type: "integer", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_event_creation_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_team_event_creation_requests_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "organization",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_event_creation_requests_team_id_status",
                schema: "organization",
                table: "team_event_creation_requests",
                columns: new[] { "team_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_event_creation_requests",
                schema: "organization");

            migrationBuilder.DropColumn(
                name: "active_event_count",
                schema: "organization",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "archived_event_count",
                schema: "organization",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "cancelled_event_count",
                schema: "organization",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "pending_event_count",
                schema: "organization",
                table: "teams");

            migrationBuilder.AddColumn<int>(
                name: "ticketed_event_scope_version",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ticketed_events",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_url = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    website_url = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticketed_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticketed_events_team_id_slug",
                schema: "organization",
                table: "ticketed_events",
                columns: new[] { "team_id", "slug" },
                unique: true);
        }
    }
}
