using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

public partial class RemoveReconfirmCadenceAddQuietHours : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "reconfirm_cadence_hours",
            schema: "email",
            table: "event_email_context_view");

        migrationBuilder.AddColumn<TimeOnly>(
            name: "reconfirm_quiet_hours_end",
            schema: "email",
            table: "event_email_context_view",
            type: "time without time zone",
            nullable: true);

        migrationBuilder.AddColumn<TimeOnly>(
            name: "reconfirm_quiet_hours_start",
            schema: "email",
            table: "event_email_context_view",
            type: "time without time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "reconfirm_quiet_hours_end",
            schema: "email",
            table: "event_email_context_view");

        migrationBuilder.DropColumn(
            name: "reconfirm_quiet_hours_start",
            schema: "email",
            table: "event_email_context_view");

        migrationBuilder.AddColumn<int>(
            name: "reconfirm_cadence_hours",
            schema: "email",
            table: "event_email_context_view",
            type: "integer",
            nullable: true);
    }
}
