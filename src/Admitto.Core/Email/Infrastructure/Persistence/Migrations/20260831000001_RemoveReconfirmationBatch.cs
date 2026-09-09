using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

public partial class RemoveReconfirmationBatch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
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

        migrationBuilder.DropTable(
            name: "reconfirmation_batches",
            schema: "email");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "reconfirmation_batches",
            schema: "email",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                team_id = table.Column<Guid>(type: "uuid", nullable: false),
                ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                last_error = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reconfirmation_batches", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_reconfirmation_batches_active_event",
            schema: "email",
            table: "reconfirmation_batches",
            column: "ticketed_event_id",
            unique: true,
            filter: "status IN ('Pending', 'Sending')");

        migrationBuilder.CreateIndex(
            name: "IX_reconfirmation_batches_event_created_at",
            schema: "email",
            table: "reconfirmation_batches",
            columns: new[] { "ticketed_event_id", "created_at" },
            descending: new[] { false, true });

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
}
