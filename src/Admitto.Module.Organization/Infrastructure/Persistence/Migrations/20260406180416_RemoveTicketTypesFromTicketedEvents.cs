using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTicketTypesFromTicketedEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ticket_types",
                schema: "organization",
                table: "ticketed_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ticket_types",
                schema: "organization",
                table: "ticketed_events",
                type: "jsonb",
                nullable: true);
        }
    }
}
