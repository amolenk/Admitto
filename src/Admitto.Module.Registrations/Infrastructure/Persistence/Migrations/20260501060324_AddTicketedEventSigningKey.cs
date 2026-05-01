using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketedEventSigningKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the column as nullable so existing rows survive the schema change;
            // backfill them with freshly-generated keys before flipping to NOT NULL.
            migrationBuilder.AddColumn<string>(
                name: "signing_key",
                schema: "registrations",
                table: "ticketed_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");

            // Idempotent backfill: only touches rows the column was just added to.
            migrationBuilder.Sql(
                "UPDATE registrations.ticketed_events " +
                "SET signing_key = encode(gen_random_bytes(32), 'base64') " +
                "WHERE signing_key IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "signing_key",
                schema: "registrations",
                table: "ticketed_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "signing_key",
                schema: "registrations",
                table: "ticketed_events");
        }
    }
}
