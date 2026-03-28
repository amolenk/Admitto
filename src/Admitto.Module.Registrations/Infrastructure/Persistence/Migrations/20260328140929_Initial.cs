using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "registrations");

            migrationBuilder.CreateTable(
                name: "coupons",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    allowed_ticket_type_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    bypass_registration_window = table.Column<bool>(type: "boolean", nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coupons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "registrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coupons_code",
                schema: "registrations",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coupons_event_id",
                schema: "registrations",
                table: "coupons",
                column: "event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupons",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "registrations");
        }
    }
}
