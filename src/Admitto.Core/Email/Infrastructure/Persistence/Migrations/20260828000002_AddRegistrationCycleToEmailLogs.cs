using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

public partial class AddRegistrationCycleToEmailLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "registration_cycle_id",
            schema: "email",
            table: "email_log",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE email.email_log SET registration_cycle_id = registration_id WHERE registration_id IS NOT NULL AND registration_cycle_id IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "registration_cycle_id",
            schema: "email",
            table: "email_log");
    }
}
