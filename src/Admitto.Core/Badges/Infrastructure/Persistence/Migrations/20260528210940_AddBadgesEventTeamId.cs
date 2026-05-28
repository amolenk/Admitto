using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgesEventTeamId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "team_id",
                schema: "badges",
                table: "badges_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_schema = 'registrations' AND table_name = 'ticketed_events'
                    ) THEN
                        UPDATE badges.badges_events be
                        SET team_id = te.team_id
                        FROM registrations.ticketed_events te
                        WHERE be.event_id = te.id;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "team_id",
                schema: "badges",
                table: "badges_events");
        }
    }
}
