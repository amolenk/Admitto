using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameBulkEmailSourceToAttendeeFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "source",
                schema: "email",
                table: "bulk_email_jobs",
                newName: "attendee_filter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "attendee_filter",
                schema: "email",
                table: "bulk_email_jobs",
                newName: "source");
        }
    }
}
