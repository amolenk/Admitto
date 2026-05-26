using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "registrations",
                table: "coupons",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source",
                schema: "registrations",
                table: "coupons");
        }
    }
}
