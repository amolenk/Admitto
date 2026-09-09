using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations;

public partial class AddRegistrationCancelledAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "cancelled_at",
            schema: "registrations",
            table: "registrations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE registrations.registrations SET cancelled_at = last_changed_at WHERE status = 'Cancelled' AND cancelled_at IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "cancelled_at",
            schema: "registrations",
            table: "registrations");
    }
}
