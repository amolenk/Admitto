using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateNameToBulkEmailJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "template_name",
                schema: "email",
                table: "bulk_email_jobs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "template_name",
                schema: "email",
                table: "bulk_email_jobs");
        }
    }
}
