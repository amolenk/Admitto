using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationIdToEmailLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "registration_id",
                schema: "email",
                table: "email_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_registration",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "registration_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_log_event_registration",
                schema: "email",
                table: "email_log");

            migrationBuilder.DropColumn(
                name: "registration_id",
                schema: "email",
                table: "email_log");
        }
    }
}
