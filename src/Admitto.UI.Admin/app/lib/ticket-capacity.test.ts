import { describe, expect, it } from "vitest";

import { ticketTypeDto } from "@/test-utils/builders";

import { formatCapacitySummary, ticketCapacity, totalTicketCapacity } from "./ticket-capacity";

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
