using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

public partial class LinkReconfirmationEmailLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "reconfirmation_batch_id",
            schema: "email",
            table: "email_log",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_email_log_reconfirmation_batch_id",
            schema: "email",
            table: "email_log",
            column: "reconfirmation_batch_id");

        migrationBuilder.AddForeignKey(
            name: "FK_email_log_reconfirmation_batches_reconfirmation_batch_id",
            schema: "email",
            table: "email_log",
            column: "reconfirmation_batch_id",
            principalSchema: "email",
            principalTable: "reconfirmation_batches",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_email_log_reconfirmation_batches_reconfirmation_batch_id",
            schema: "email",
            table: "email_log");

        migrationBuilder.DropIndex(
            name: "IX_email_log_reconfirmation_batch_id",
            schema: "email",
            table: "email_log");

        migrationBuilder.DropColumn(
            name: "reconfirmation_batch_id",
            schema: "email",
            table: "email_log");
    }
}
