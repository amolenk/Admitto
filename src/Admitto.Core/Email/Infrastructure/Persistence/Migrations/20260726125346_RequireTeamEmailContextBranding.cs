using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequireTeamEmailContextBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No code path produces a partially-projected team row: both source events
            // (TeamCreated, TeamDetailsUpdated) carry the complete field set. Any such row
            // would therefore be a leftover carrying no information beyond its team id.
            // Drop them rather than coercing to an empty string, which would fail
            // AccentColor validation on read. Absence is handled by the send pipeline,
            // which falls back to default branding and the system sender label.
            migrationBuilder.Sql(
                """
                DELETE FROM email.team_email_context_view
                WHERE team_name IS NULL OR accent_color IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "team_name",
                schema: "email",
                table: "team_email_context_view",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "accent_color",
                schema: "email",
                table: "team_email_context_view",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "team_name",
                schema: "email",
                table: "team_email_context_view",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "accent_color",
                schema: "email",
                table: "team_email_context_view",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7);
        }
    }
}
