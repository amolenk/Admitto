using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations;

public partial class AddRegistrationCycleId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "registration_cycle_id",
            schema: "registrations",
            table: "registrations",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE registrations.registrations SET registration_cycle_id = id WHERE registration_cycle_id IS NULL;");

        migrationBuilder.AlterColumn<Guid>(
            name: "registration_cycle_id",
            schema: "registrations",
            table: "registrations",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "registration_cycle_id",
            schema: "registrations",
            table: "registrations");
    }
}
