using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.Migrations;

public partial class AddReconfirmPolicyCloseEvaluation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "reconfirm_policy_close_evaluations",
            schema: "email",
            columns: table => new
            {
                team_id = table.Column<Guid>(type: "uuid", nullable: false),
                ticketed_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                closes_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                evaluated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reconfirm_policy_close_evaluations", x => new
                {
                    x.team_id,
                    x.ticketed_event_id,
                    x.closes_at
                });
            });

        migrationBuilder.CreateIndex(
            name: "IX_reconfirm_policy_close_evaluations_event_close",
            schema: "email",
            table: "reconfirm_policy_close_evaluations",
            columns: new[] { "ticketed_event_id", "closes_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "reconfirm_policy_close_evaluations",
            schema: "email");
    }
}
