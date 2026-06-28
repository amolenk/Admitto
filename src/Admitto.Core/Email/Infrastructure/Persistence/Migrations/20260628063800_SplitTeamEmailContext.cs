using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitTeamEmailContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "team_accent_color",
                schema: "email",
                table: "event_email_context_view");

            migrationBuilder.AddColumn<long>(
                name: "ticket_catalog_version",
                schema: "email",
                table: "event_email_context_view",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ticketed_event_version",
                schema: "email",
                table: "event_email_context_view",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "email",
                table: "event_email_context_view",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "team_email_context_view",
                schema: "email",
                columns: table => new
                {
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    accent_color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    team_version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_email_context_view", x => x.team_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_email_context_view",
                schema: "email");

            migrationBuilder.DropColumn(
                name: "ticket_catalog_version",
                schema: "email",
                table: "event_email_context_view");

            migrationBuilder.DropColumn(
                name: "ticketed_event_version",
                schema: "email",
                table: "event_email_context_view");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "email",
                table: "event_email_context_view");

            migrationBuilder.AddColumn<string>(
                name: "team_accent_color",
                schema: "email",
                table: "event_email_context_view",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }
    }
}
