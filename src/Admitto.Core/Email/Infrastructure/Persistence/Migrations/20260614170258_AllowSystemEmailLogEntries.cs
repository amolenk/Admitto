using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowSystemEmailLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_log_event_recipient_idempotency",
                schema: "email",
                table: "email_log");

            migrationBuilder.AlterColumn<Guid>(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "team_id",
                schema: "email",
                table: "email_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_recipient_idempotency",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "recipient", "idempotency_key" },
                unique: true,
                filter: "ticketed_event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_email_log_system_recipient_idempotency",
                schema: "email",
                table: "email_log",
                columns: new[] { "recipient", "idempotency_key" },
                unique: true,
                filter: "ticketed_event_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_email_log_event_recipient_idempotency",
                schema: "email",
                table: "email_log");

            migrationBuilder.DropIndex(
                name: "IX_email_log_system_recipient_idempotency",
                schema: "email",
                table: "email_log");

            migrationBuilder.AlterColumn<Guid>(
                name: "ticketed_event_id",
                schema: "email",
                table: "email_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "team_id",
                schema: "email",
                table: "email_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_log_event_recipient_idempotency",
                schema: "email",
                table: "email_log",
                columns: new[] { "ticketed_event_id", "recipient", "idempotency_key" },
                unique: true);
        }
    }
}
