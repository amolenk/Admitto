using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTeamEmailAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_address",
                schema: "organization",
                table: "teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email_address",
                schema: "organization",
                table: "teams",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");
        }
    }
}
