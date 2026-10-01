using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PublicCapacityOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "waitlist_origin",
                schema: "registrations",
                table: "coupons",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // There is no live data, so the statements below are expected to touch no rows. They exist so that any
            // data found by the pre-deploy check still maps onto the public-capacity model (ADR-019).

            // Waitlist coupons carry their origin so a redemption knows its pool. Copy it from the waitlist's own
            // coupon record; rows written before origins were tracked lack the key and count as Automatic.
            migrationBuilder.Sql(@"
UPDATE registrations.coupons c
SET waitlist_origin = COALESCE(
    (SELECT wc->>'origin'
     FROM registrations.waitlists w, jsonb_array_elements(COALESCE(w.waitlist_coupons, '[]'::jsonb)) wc
     WHERE wc->>'id' = c.id::text
     LIMIT 1),
    'Automatic')
WHERE c.source = 'Waitlist';");

            // Ticket types: max_capacity minus the unspent reserved_capacity becomes public_capacity (the portion
            // held back for admin/coupon claims was never available to the public pool), and reserved_used_capacity
            // becomes admin_used_count. used_capacity counted reserved claims too, so the public count is what
            // remains once those are moved to the admin pool. reserved_capacity is dropped.
            migrationBuilder.Sql(@"
UPDATE registrations.ticket_catalog
SET ticket_types = (
    SELECT COALESCE(jsonb_agg(
        (tt - 'max_capacity' - 'used_capacity' - 'reserved_capacity' - 'reserved_used_capacity')
        || jsonb_build_object(
            'public_capacity', CASE
                WHEN tt->'max_capacity' IS NULL THEN 'null'::jsonb
                ELSE to_jsonb(GREATEST(0,
                    (tt->>'max_capacity')::int - COALESCE((tt->>'reserved_capacity')::int, 0)))
            END,
            'public_used_capacity', GREATEST(0,
                COALESCE((tt->>'used_capacity')::int, 0) - COALESCE((tt->>'reserved_used_capacity')::int, 0)),
            'admin_used_count', COALESCE((tt->>'reserved_used_capacity')::int, 0))
        ORDER BY ord), '[]'::jsonb)
    FROM jsonb_array_elements(ticket_types) WITH ORDINALITY AS t(tt, ord))
WHERE jsonb_typeof(ticket_types) = 'array'
  AND EXISTS (SELECT 1 FROM jsonb_array_elements(ticket_types) e WHERE e ? 'used_capacity');");

            // Registration tickets: Reserved becomes Admin and PublicUncapped becomes Public.
            migrationBuilder.Sql(@"
UPDATE registrations.registrations
SET tickets = (
    SELECT COALESCE(jsonb_agg(
        CASE t->>'mode'
            WHEN 'Reserved' THEN jsonb_set(t, '{mode}', '""Admin""')
            WHEN 'PublicUncapped' THEN jsonb_set(t, '{mode}', '""Public""')
            ELSE t
        END
        ORDER BY ord), '[]'::jsonb)
    FROM jsonb_array_elements(tickets) WITH ORDINALITY AS x(t, ord))
WHERE jsonb_typeof(tickets) = 'array'
  AND EXISTS (SELECT 1 FROM jsonb_array_elements(tickets) e WHERE e->>'mode' IN ('Reserved', 'PublicUncapped'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "waitlist_origin",
                schema: "registrations",
                table: "coupons");
        }
    }
}
