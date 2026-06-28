using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventEmailContextView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_email_context_view",
                schema: "email",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_accent_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_email_context_view", x => new { x.team_id, x.ticketed_event_id });
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_email_context_view",
                schema: "email");
        }
    }
}
