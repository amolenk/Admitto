using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeInstanceScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "event_id",
                schema: "badges",
                table: "badge_instances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "team_id",
                schema: "badges",
                table: "badge_instances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_badge_instances_team_id_event_id_badge_type_id",
                schema: "badges",
                table: "badge_instances",
                columns: new[] { "team_id", "event_id", "badge_type_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_badge_instances_team_id_event_id_badge_type_id",
                schema: "badges",
                table: "badge_instances");

            migrationBuilder.DropColumn(
                name: "event_id",
                schema: "badges",
                table: "badge_instances");

            migrationBuilder.DropColumn(
                name: "team_id",
                schema: "badges",
                table: "badge_instances");
        }
    }
}
