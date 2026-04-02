using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamArchiveColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                schema: "organization",
                table: "teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ticketed_event_scope_version",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archived_at",
                schema: "organization",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "ticketed_event_scope_version",
                schema: "organization",
                table: "teams");
        }
    }
}
