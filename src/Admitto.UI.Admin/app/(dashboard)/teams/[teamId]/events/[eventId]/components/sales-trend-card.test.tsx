import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { registrationListItemDto } from "@/test-utils/builders";

import { buildBuckets } from "./sales-trend-card";

describe("buildBuckets", () => {
    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date("2027-06-14T10:00:00.000Z"));
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    // Given a cancelled registration created and cancelled on different days
    // When sales buckets are built
    // Then the cancellation is placed on its cancellation date
    it("buckets cancellations by cancelledAt", () => {
        const registrations = [
            registrationListItemDto({
                status: "cancelled",
                createdAt: "2027-06-03T10:00:00.000Z",
                cancelledAt: "2027-06-08T10:00:00.000Z",
            }),
        ];

        const buckets = buildBuckets(registrations, "14d");

        expect(buckets.find((bucket) => bucket.date === "2027-06-03")?.cancellations).toBe(0);
        expect(buckets.find((bucket) => bucket.date === "2027-06-08")?.cancellations).toBe(1);
    });

    // Given a registered registration
    // When sales buckets are built
    // Then it remains placed on its creation date
    it("buckets registrations by createdAt", () => {
        const registrations = [
            registrationListItemDto({
                status: "registered",
                createdAt: "2027-06-03T10:00:00.000Z",
            }),
        ];

        const buckets = buildBuckets(registrations, "14d");

        expect(buckets.find((bucket) => bucket.date === "2027-06-03")?.registrations).toBe(1);
    });

    // Given a cancelled registration without a cancellation date
    // When sales buckets are built
    // Then it is omitted from the cancellations series
    it("skips cancellations without cancelledAt", () => {
        const registrations = [
            registrationListItemDto({ status: "cancelled", cancelledAt: null }),
        ];

        const buckets = buildBuckets(registrations, "14d");

        expect(buckets.every((bucket) => bucket.cancellations === 0)).toBe(true);
    });
});
