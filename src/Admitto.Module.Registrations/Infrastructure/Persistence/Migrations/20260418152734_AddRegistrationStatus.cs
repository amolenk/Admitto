using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "registration_status",
                schema: "registrations",
                table: "event_registration_policy",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Open");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "registration_status",
                schema: "registrations",
                table: "event_registration_policy");
        }
    }
}
