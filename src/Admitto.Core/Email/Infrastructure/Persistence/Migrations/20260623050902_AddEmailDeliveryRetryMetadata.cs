using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryRetryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "email",
                table: "outbox",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "delivery_attempt_count",
                schema: "email",
                table: "email_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_pending_created_at",
                schema: "email",
                table: "outbox",
                columns: new[] { "state", "created_at" },
                filter: "state = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_pending_created_at",
                schema: "email",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "email",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "delivery_attempt_count",
                schema: "email",
                table: "email_log");
        }
    }
}
