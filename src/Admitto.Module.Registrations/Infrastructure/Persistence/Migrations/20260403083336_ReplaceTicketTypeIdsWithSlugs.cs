using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTicketTypeIdsWithSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowed_ticket_type_ids",
                schema: "registrations",
                table: "coupons");

            migrationBuilder.AddColumn<string[]>(
                name: "allowed_ticket_type_slugs",
                schema: "registrations",
                table: "coupons",
                type: "text[]",
                maxLength: 64,
                nullable: false,
                defaultValue: new string[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allowed_ticket_type_slugs",
                schema: "registrations",
                table: "coupons");

            migrationBuilder.AddColumn<Guid[]>(
                name: "allowed_ticket_type_ids",
                schema: "registrations",
                table: "coupons",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);
        }
    }
}
