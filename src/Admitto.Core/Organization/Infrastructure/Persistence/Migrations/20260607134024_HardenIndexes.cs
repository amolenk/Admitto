using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_deprovision_after",
                schema: "organization",
                table: "users",
                column: "deprovision_after",
                filter: "deprovision_after IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_state",
                schema: "organization",
                table: "outbox",
                column: "state",
                filter: "state = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_deprovision_after",
                schema: "organization",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_outbox_state",
                schema: "organization",
                table: "outbox");
        }
    }
}
