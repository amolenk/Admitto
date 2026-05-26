using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WaitlistAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "waitlists",
                schema: "registrations",
                columns: table => new
                {
                    ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    entries = table.Column<string>(type: "jsonb", nullable: true),
                    waitlist_coupons = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitlists", x => x.ticket_type_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_waitlists_event_id",
                schema: "registrations",
                table: "waitlists",
                column: "event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "waitlists",
                schema: "registrations");
        }
    }
}
