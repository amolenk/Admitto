using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistQuietHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "quiet_hours_end",
                schema: "registrations",
                table: "ticketed_events",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(8, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "quiet_hours_start",
                schema: "registrations",
                table: "ticketed_events",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(22, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quiet_hours_end",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropColumn(
                name: "quiet_hours_start",
                schema: "registrations",
                table: "ticketed_events");
        }
    }
}
