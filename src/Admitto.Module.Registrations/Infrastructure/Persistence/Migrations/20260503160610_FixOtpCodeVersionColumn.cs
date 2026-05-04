using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOtpCodeVersionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                schema: "registrations",
                table: "otp_codes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "version",
                schema: "registrations",
                table: "otp_codes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }
    }
}
