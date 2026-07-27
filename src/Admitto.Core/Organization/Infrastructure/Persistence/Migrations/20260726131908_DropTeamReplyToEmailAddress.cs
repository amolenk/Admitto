using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropTeamReplyToEmailAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reply_to_email_address",
                schema: "organization",
                table: "teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reply_to_email_address",
                schema: "organization",
                table: "teams",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);
        }
    }
}
