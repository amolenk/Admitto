using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BadgeEventAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "badge_types",
                schema: "badges");

            migrationBuilder.AddColumn<string>(
                name: "badge_types",
                schema: "badges",
                table: "badges_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "badges",
                table: "badges_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_changed_at",
                schema: "badges",
                table: "badges_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "last_changed_by",
                schema: "badges",
                table: "badges_events",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "badges",
                table: "badges_events",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "badge_types",
                schema: "badges",
                table: "badges_events");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "badges",
                table: "badges_events");

            migrationBuilder.DropColumn(
                name: "last_changed_at",
                schema: "badges",
                table: "badges_events");

            migrationBuilder.DropColumn(
                name: "last_changed_by",
                schema: "badges",
                table: "badges_events");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "badges",
                table: "badges_events");

            migrationBuilder.CreateTable(
                name: "badge_types",
                schema: "badges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    last_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_changed_by = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ticket_type_ids = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badge_types", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_badge_types_event_id_name",
                schema: "badges",
                table: "badge_types",
                columns: new[] { "event_id", "name" },
                unique: true);
        }
    }
}
