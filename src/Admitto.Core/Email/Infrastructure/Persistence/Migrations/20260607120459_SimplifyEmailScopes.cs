using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEmailScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_settings_scope_scope_id",
                schema: "email",
                table: "email_settings");

            migrationBuilder.Sql("DROP INDEX IF EXISTS email.IX_email_templates_scope_scope_id_name;");

            migrationBuilder.AddColumn<Guid>(
                name: "team_id",
                schema: "email",
                table: "email_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "team_id",
                schema: "email",
                table: "email_settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE email.email_settings
                SET team_id = scope_id
                WHERE scope = 0;

                UPDATE email.email_templates
                SET team_id = scope_id
                WHERE scope = 0;

                DO $$
                BEGIN
                    IF to_regclass('registrations.ticketed_events') IS NOT NULL THEN
                        EXECUTE '
                            UPDATE email.email_settings s
                            SET team_id = e.team_id,
                                ticketed_event_id = s.scope_id
                            FROM registrations.ticketed_events e
                            WHERE s.scope = 1
                              AND e.id = s.scope_id';

                        EXECUTE '
                            UPDATE email.email_templates t
                            SET team_id = e.team_id,
                                ticketed_event_id = t.scope_id
                            FROM registrations.ticketed_events e
                            WHERE t.scope = 1
                              AND e.id = t.scope_id';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "scope",
                schema: "email",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "scope_id",
                schema: "email",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "scope",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropColumn(
                name: "scope_id",
                schema: "email",
                table: "email_settings");

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_team",
                schema: "email",
                table: "email_settings",
                column: "team_id",
                unique: true,
                filter: "ticketed_event_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_team_event",
                schema: "email",
                table: "email_settings",
                columns: new[] { "team_id", "ticketed_event_id" },
                unique: true,
                filter: "ticketed_event_id IS NOT NULL");

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_email_templates_team_name
                ON email.email_templates (team_id, lower(name))
                WHERE ticketed_event_id IS NULL;

                CREATE UNIQUE INDEX IX_email_templates_team_event_name
                ON email.email_templates (team_id, ticketed_event_id, lower(name))
                WHERE ticketed_event_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_settings_team",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropIndex(
                name: "IX_email_settings_team_event",
                schema: "email",
                table: "email_settings");

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS email.IX_email_templates_team_name;
                DROP INDEX IF EXISTS email.IX_email_templates_team_event_name;
                """);

            migrationBuilder.AddColumn<int>(
                name: "scope",
                schema: "email",
                table: "email_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "scope",
                schema: "email",
                table: "email_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                schema: "email",
                table: "email_templates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "scope_id",
                schema: "email",
                table: "email_settings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                UPDATE email.email_settings
                SET scope = CASE WHEN ticketed_event_id IS NULL THEN 0 ELSE 1 END,
                    scope_id = COALESCE(ticketed_event_id, team_id);

                UPDATE email.email_templates
                SET scope = CASE WHEN ticketed_event_id IS NULL THEN 0 ELSE 1 END,
                    scope_id = COALESCE(ticketed_event_id, team_id);
                """);

            migrationBuilder.DropColumn(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "team_id",
                schema: "email",
                table: "email_templates");

            migrationBuilder.DropColumn(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_settings");

            migrationBuilder.DropColumn(
                name: "team_id",
                schema: "email",
                table: "email_settings");

            migrationBuilder.CreateIndex(
                name: "IX_email_settings_scope_scope_id",
                schema: "email",
                table: "email_settings",
                columns: new[] { "scope", "scope_id" },
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IX_email_templates_scope_scope_id_name
                ON email.email_templates (scope, scope_id, lower(name));
                """);
        }
    }
}
