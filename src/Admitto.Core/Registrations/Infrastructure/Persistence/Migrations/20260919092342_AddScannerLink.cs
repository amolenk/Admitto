using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScannerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scanner_link_created_at",
                schema: "registrations",
                table: "ticketed_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scanner_link_revoked_at",
                schema: "registrations",
                table: "ticketed_events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scanner_link_secret",
                schema: "registrations",
                table: "ticketed_events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scanner_link_created_at",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropColumn(
                name: "scanner_link_revoked_at",
                schema: "registrations",
                table: "ticketed_events");

            migrationBuilder.DropColumn(
                name: "scanner_link_secret",
                schema: "registrations",
                table: "ticketed_events");
        }
    }
}
