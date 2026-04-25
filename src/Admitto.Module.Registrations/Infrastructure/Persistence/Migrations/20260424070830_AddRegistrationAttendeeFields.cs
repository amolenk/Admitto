using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAttendeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                schema: "registrations",
                table: "registrations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "registrations",
                table: "registrations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "has_reconfirmed",
                schema: "registrations",
                table: "registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "registrations",
                table: "registrations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reconfirmed_at",
                schema: "registrations",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "registrations",
                table: "registrations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "has_reconfirmed",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "reconfirmed_at",
                schema: "registrations",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "registrations",
                table: "registrations");
        }
    }
}
