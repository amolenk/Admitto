using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEmailTemplating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_templates",
                schema: "email");

            migrationBuilder.DropIndex(
                name: "IX_email_settings_team",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropIndex(
                name: "IX_email_settings_team_event",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropColumn(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropColumn(
                name: "template_name",
                schema: "email",
                table: "bulk_email_jobs");

            migrationBuilder.AddColumn<string>(
                name: "accent_color",
                schema: "email",
                table: "email_settings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "#2563eb");

            migrationBuilder.AddColumn<string>(
                name: "font_family",
                schema: "email",
                table: "email_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Arial, sans-serif");

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_team",
                schema: "email",
                table: "email_settings",
                column: "team_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_settings_team",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropColumn(
                name: "accent_color",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropColumn(
                name: "font_family",
                schema: "email",
                table: "email_settings");

            migrationBuilder.AddColumn<Guid>(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "template_name",
                schema: "email",
                table: "bulk_email_jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_templates",
                schema: "email",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_body = table.Column<string>(type: "text", nullable: false),
                    ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_team",
                schema: "email",
                table: "email_settings",
                column: "team_id",
                unique: true,
                filter: "ticketed_event_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_team_event",
                schema: "email",
                table: "email_settings",
                columns: new[] { "team_id", "ticketed_event_id" },
                unique: true,
                filter: "ticketed_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_team_id_ticketed_event_id",
                schema: "email",
                table: "email_templates",
                columns: new[] { "team_id", "ticketed_event_id" });
        }
    }
}
