using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoEventSlugToCreationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "public_slug",
                schema: "organization",
                table: "team_event_creation_requests",
                type: "text",
                nullable: false,
                defaultValue: "legacy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "public_slug",
                schema: "organization",
                table: "team_event_creation_requests");
        }
    }
}
