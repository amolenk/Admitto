using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCouponRevoke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WaitlistCoupon.ExpiresAt lives in the waitlist_coupons JSON column, so it needs no schema change.
            // No backfill either: there is no live data holding waitlist coupons without "expires_at".
            migrationBuilder.DropColumn(
                name: "revoked_at",
                schema: "registrations",
                table: "coupons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "revoked_at",
                schema: "registrations",
                table: "coupons",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
