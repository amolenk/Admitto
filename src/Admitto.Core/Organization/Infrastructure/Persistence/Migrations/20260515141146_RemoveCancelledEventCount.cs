using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCancelledEventCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate any creation requests that observed EventStatus.Cancelled (1) → Archived (2)
            migrationBuilder.Sql(
                "UPDATE organization.team_event_creation_requests SET observed_event_status = 2 " +
                "WHERE observed_event_status = 1;");

            migrationBuilder.DropColumn(
                name: "cancelled_event_count",
                schema: "organization",
                table: "teams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cancelled_event_count",
                schema: "organization",
                table: "teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
