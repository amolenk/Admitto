using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropTemplateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill name from the old type slug for built-in templates.
            migrationBuilder.Sql("""
                UPDATE email.email_templates
                SET name = CASE type
                    WHEN 'ticket'             THEN 'Ticket confirmation'
                    WHEN 'reconfirm'          THEN 'Reconfirmation'
                    WHEN 'cancellation'       THEN 'Cancellation'
                    WHEN 'visa-letter-denied' THEN 'Visa letter denied'
                    WHEN 'otp-code'           THEN 'Verification code'
                    ELSE name
                END
                WHERE name IS NULL OR name = '';
                """);

            // Backfill bulk_email_jobs.email_type from old slugs to new reserved names.
            migrationBuilder.Sql("""
                UPDATE email.bulk_email_jobs
                SET email_type = CASE email_type
                    WHEN 'ticket'             THEN 'Ticket confirmation'
                    WHEN 'reconfirm'          THEN 'Reconfirmation'
                    WHEN 'cancellation'       THEN 'Cancellation'
                    WHEN 'visa-letter-denied' THEN 'Visa letter denied'
                    WHEN 'otp-code'           THEN 'Verification code'
                    ELSE email_type
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "type",
                schema: "email",
                table: "email_templates");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "email",
                table: "email_templates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Functional case-insensitive unique index on (scope, scope_id, lower(name)).
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IX_email_templates_scope_scope_id_name
                ON email.email_templates (scope, scope_id, lower(name));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS email.IX_email_templates_scope_scope_id_name;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "email",
                table: "email_templates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "type",
                schema: "email",
                table: "email_templates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_scope_scope_id_type",
                schema: "email",
                table: "email_templates",
                columns: new[] { "scope", "scope_id", "type" },
                unique: true,
                filter: "name IS NULL");
        }
    }
}
