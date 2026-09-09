using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations;

public partial class RemoveReconfirmCadenceAddQuietHours : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "reconfirm_policy_cadence",
            schema: "registrations",
            table: "ticketed_events");

        migrationBuilder.AddColumn<TimeOnly>(
            name: "reconfirm_policy_quiet_hours_end",
            schema: "registrations",
            table: "ticketed_events",
            type: "time without time zone",
            nullable: true);

        migrationBuilder.AddColumn<TimeOnly>(
            name: "reconfirm_policy_quiet_hours_start",
            schema: "registrations",
            table: "ticketed_events",
            type: "time without time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "reconfirm_policy_quiet_hours_end",
            schema: "registrations",
            table: "ticketed_events");

        migrationBuilder.DropColumn(
            name: "reconfirm_policy_quiet_hours_start",
            schema: "registrations",
            table: "ticketed_events");

        migrationBuilder.AddColumn<TimeSpan>(
            name: "reconfirm_policy_cadence",
            schema: "registrations",
            table: "ticketed_events",
            type: "interval",
            nullable: true);
    }
}
