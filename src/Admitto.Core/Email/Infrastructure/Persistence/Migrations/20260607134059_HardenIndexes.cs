using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                schema: "email",
                table: "email_log",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state",
                schema: "email",
                table: "outbox",
                column: "state",
                filter: "state = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_team_id_ticketed_event_id",
                schema: "email",
                table: "email_templates",
                columns: new[] { "team_id", "ticketed_event_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_state",
                schema: "email",
                table: "outbox");

            migrationBuilder.DropIndex(
                name: "IX_email_templates_team_id_ticketed_event_id",
                schema: "email",
                table: "email_templates");

            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                schema: "email",
                table: "email_log",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);
        }
    }
}
