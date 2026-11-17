import { describe, expect, it } from "vitest";

import { ticketTypeDto } from "@/test-utils/builders";

import {
    formatCapacitySummary,
    previewPublicCapacityChange,
    ticketCapacity,
    totalTicketCapacity,
} from "./ticket-capacity";

describe("ticketCapacity", () => {
    // Given a ticket type with admin tickets on top of a full public capacity
    // When its capacity is read
    // Then it is publicly sold out and the admin tickets only add to the total
    it("counts admin tickets on top of the public capacity", () => {
        const c = ticketCapacity(ticketTypeDto({ publicCapacity: 10, publicUsedCapacity: 10, adminUsedCount: 4 }));

        expect(c).toEqual({
            publicCapacity: 10,
            publicUsed: 10,
            adminUsed: 4,
            total: 14,
            effectiveCapacity: 14,
            publicRemaining: 0,
            isPubliclySoldOut: true,
            publicUsedPercent: 100,
        });
    });

    // Given a ticket type with a public capacity of zero
    // When its capacity is read
    // Then it is publicly sold out rather than unlimited
    it("treats a public capacity of zero as sold out, not unlimited", () => {
        const c = ticketCapacity(ticketTypeDto({ publicCapacity: 0, adminUsedCount: 2 }));

        expect(c.publicCapacity).toBe(0);
        expect(c.isPubliclySoldOut).toBe(true);
        expect(c.publicUsedPercent).toBe(100);
    });

    // Given a ticket type without a public capacity
    // When its capacity is read
    // Then it is never sold out
    it("treats a missing public capacity as unlimited", () => {
        const c = ticketCapacity(ticketTypeDto({ publicCapacity: null, publicUsedCapacity: 7 }));

        expect(c.publicRemaining).toBeNull();
        expect(c.isPubliclySoldOut).toBe(false);
        expect(c.publicUsedPercent).toBeNull();
    });
});

describe("totalTicketCapacity", () => {
    // Given a bounded and an unlimited ticket type
    // When their capacity is summed
    // Then the public capacity is unlimited and the counts add up
    it("sums counts and is unlimited when any ticket type is", () => {
        const c = totalTicketCapacity([
            ticketTypeDto({ publicCapacity: 10, publicUsedCapacity: 4, adminUsedCount: 1 }),
            ticketTypeDto({ publicCapacity: null, publicUsedCapacity: 3, adminUsedCount: 2 }),
        ]);

        expect(c.publicCapacity).toBeNull();
        expect(c.publicUsed).toBe(7);
        expect(c.adminUsed).toBe(3);
        expect(c.total).toBe(10);
    });
});

describe("formatCapacitySummary", () => {
    // Given public and admin counts with a bounded public capacity
    // When they are formatted
    // Then the summary shows total issued over effective capacity (public capacity plus admin issued)
    it("formats total issued over effective capacity", () => {
        const summary = formatCapacitySummary(
            ticketCapacity(ticketTypeDto({ publicCapacity: 100, publicUsedCapacity: 25, adminUsedCount: 3 })));

        expect(summary).toBe("28/103");
    });

    // Given an unlimited public capacity
    // When it is formatted
    // Then the effective capacity shows as infinity
    it("shows an unlimited effective capacity as infinity", () => {
        const summary = formatCapacitySummary(
            ticketCapacity(ticketTypeDto({ publicCapacity: null, publicUsedCapacity: 7 })));

        expect(summary).toBe("7/∞");
    });
});

describe("previewPublicCapacityChange", () => {
    const waiting = { publicCapacity: 7, publicUsedCapacity: 7, adminUsedCount: 8, waitlistEnabled: true, waitlistMode: true, waitlistQueuedCount: 6 };

    // Given 7 public seats taken, 8 admin tickets on top and 6 people waiting
    // When the public capacity is raised to 15
    // Then everyone waiting gets an offer and 2 seats are left over, because admin tickets don't count
    it("offers the freed public seats to the queue, ignoring admin tickets", () => {
        expect(previewPublicCapacityChange(ticketTypeDto(waiting), 15)).toEqual({ offers: 6, seatsOpening: 2 });
    });

    // Given 6 people waiting
    // When the public capacity is raised by fewer seats than people waiting
    // Then only that many offers go out
    it("caps the offers at the freed seats", () => {
        expect(previewPublicCapacityChange(ticketTypeDto(waiting), 10)).toEqual({ offers: 3, seatsOpening: 0 });
    });

    // Given outstanding offers already holding public seats
    // When the public capacity is raised
    // Then the held seats are covered before anyone else gets an offer
    it("covers seats held by outstanding offers first", () => {
        const t = ticketTypeDto({ ...waiting, publicUsedCapacity: 5, waitlistHeldCapacity: 2 });
        expect(previewPublicCapacityChange(t, 8)).toEqual({ offers: 1, seatsOpening: 0 });
    });

    // Given a ticket type that is not in waitlist mode, or a capacity that doesn't free a seat
    // When the public capacity changes
    // Then no offers are previewed
    it("returns null when the change sends no offers", () => {
        expect(previewPublicCapacityChange(ticketTypeDto({ ...waiting, waitlistMode: false }), 15)).toBeNull();
        expect(previewPublicCapacityChange(ticketTypeDto(waiting), 7)).toBeNull();
        expect(previewPublicCapacityChange(ticketTypeDto(waiting), 6)).toBeNull();
        expect(previewPublicCapacityChange(ticketTypeDto(waiting), undefined)).toBeNull();
        expect(previewPublicCapacityChange(ticketTypeDto({ ...waiting, waitlistQueuedCount: 0 }), 15)).toBeNull();
    });
});
