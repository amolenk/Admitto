using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLogScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_activity_log_registration_type_occurred",
                schema: "registrations",
                table: "activity_log");

            migrationBuilder.AddColumn<Guid>(
                name: "event_id",
                schema: "registrations",
                table: "activity_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "team_id",
                schema: "registrations",
                table: "activity_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_registration_type_occurred",
                schema: "registrations",
                table: "activity_log",
                columns: new[] { "team_id", "event_id", "registration_id", "activity_type", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_activity_log_registration_type_occurred",
                schema: "registrations",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "event_id",
                schema: "registrations",
                table: "activity_log");

            migrationBuilder.DropColumn(
                name: "team_id",
                schema: "registrations",
                table: "activity_log");

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_registration_type_occurred",
                schema: "registrations",
                table: "activity_log",
                columns: new[] { "registration_id", "activity_type", "occurred_at" });
        }
    }
}
