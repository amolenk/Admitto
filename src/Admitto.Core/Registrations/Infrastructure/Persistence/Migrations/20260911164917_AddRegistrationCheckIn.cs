using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "checked_in_at",
                schema: "registrations",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "search_text",
                schema: "registrations",
                table: "registrations",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE registrations.registrations SET search_text = lower(first_name || ' ' || last_name || ' ' || email);");

            migrationBuilder.CreateIndex(
                name: "IX_registrations_team_event_search_text",
                schema: "registrations",
                table: "registrations",
                columns: new[] { "team_id", "event_id", "search_text" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_registrations_cancelled_without_check_in",
                schema: "registrations",
                table: "registrations",
                sql: "status <> 'Cancelled' OR checked_in_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_registrations_team_event_search_text",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_registrations_cancelled_without_check_in",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "checked_in_at",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "search_text",
                schema: "registrations",
                table: "registrations");
        }
    }
}
