using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNameToEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates");

            migrationBuilder.AlterColumn<string>(
                name: "html_body",
                schema: "email",
                table: "email_templates",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "email",
                table: "email_templates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates",
                columns: new[] { "scope", "scope_id", "type" },
                unique: true,
                filter: "name IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "email",
                table: "email_templates");

            migrationBuilder.AlterColumn<string>(
                name: "html_body",
                schema: "email",
                table: "email_templates",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates",
                columns: new[] { "scope", "scope_id", "type" },
                unique: true);
        }
    }
}
