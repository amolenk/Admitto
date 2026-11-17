import * as z from "zod";
import type { TicketTypeDto } from "@/lib/admitto-api/generated";

// A ticket type's capacity in the public-capacity model (ADR-019): `publicCapacity` is the only enforced limit,
// and admin tickets (admin registrations, organiser coupons, VIP promotions) come on top of it.

/** Help text for the public capacity field on the add and edit ticket type forms. */
export const PUBLIC_CAPACITY_HELP =
    "Seats available through self-service. Admin registrations and coupons come on top of this.";

/**
 * Zod `superRefine` check shared by every ticket type form: a public capacity is required once capacity
 * is limited, since that is the only enforced limit in the public-capacity model.
 */
export function requirePublicCapacityWhenLimited(
    data: { limitCapacity: boolean; publicCapacity?: number },
    ctx: z.RefinementCtx,
): void {
    if (data.limitCapacity && data.publicCapacity === undefined) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ["publicCapacity"],
            message: "Required while capacity is limited",
        });
    }
}

export interface TicketCapacity {
    /** Seats available through self-service; `null` means no limit. */
    publicCapacity: number | null;
    publicUsed: number;
    adminUsed: number;
    /** Public plus admin tickets. Not a limit: there is no total to exceed. */
    total: number;
    /**
     * `publicCapacity` with admin tickets added on top, since admin issuance isn't capped by it
     * (ADR-019). `null` when the public capacity itself is unlimited.
     */
    effectiveCapacity: number | null;
    /** Public seats left for self-service; `null` when unlimited. */
    publicRemaining: number | null;
    /** No public seats left. Admins can still add tickets on top. */
    isPubliclySoldOut: boolean;
    /** Share of the public capacity used, 0–100; `null` when unlimited. A capacity of 0 counts as full. */
    publicUsedPercent: number | null;
}

function capacityOf(publicCapacity: number | null, publicUsed: number, adminUsed: number): TicketCapacity {
    const publicRemaining = publicCapacity == null ? null : Math.max(0, publicCapacity - publicUsed);
    const publicUsedPercent = publicCapacity == null
        ? null
        : publicCapacity === 0 ? 100 : Math.min(100, Math.round((publicUsed / publicCapacity) * 100));

    return {
        publicCapacity,
        publicUsed,
        adminUsed,
        total: publicUsed + adminUsed,
        effectiveCapacity: publicCapacity == null ? null : publicCapacity + adminUsed,
        publicRemaining,
        isPubliclySoldOut: publicRemaining === 0,
        publicUsedPercent,
    };
}

export function ticketCapacity(t: TicketTypeDto): TicketCapacity {
    return capacityOf(
        t.publicCapacity == null ? null : Number(t.publicCapacity),
        Number(t.publicUsedCapacity) || 0,
        Number(t.adminUsedCount) || 0,
    );
}

/** Sums the capacity of several ticket types; the public capacity is unlimited if any of them is. */
export function totalTicketCapacity(ticketTypes: TicketTypeDto[]): TicketCapacity {
    const capacities = ticketTypes.map(ticketCapacity);
    const publicCapacity = capacities.some((c) => c.publicCapacity == null)
        ? null
        : capacities.reduce((sum, c) => sum + (c.publicCapacity ?? 0), 0);

    return capacityOf(
        publicCapacity,
        capacities.reduce((sum, c) => sum + c.publicUsed, 0),
        capacities.reduce((sum, c) => sum + c.adminUsed, 0),
    );
}

/** `X/Y`, where X is public plus admin tickets issued and Y is the public capacity plus admin tickets issued
 *  (admin issuance isn't capped, so it raises both sides together). `∞` when the public capacity is unlimited. */
export function formatCapacitySummary(c: TicketCapacity): string {
    const effectiveCapacity = c.effectiveCapacity == null ? "∞" : String(c.effectiveCapacity);
    return `${c.total}/${effectiveCapacity}`;
}

/** What saving a new public capacity will do to the people on the ticket type's waitlist. */
export interface PublicCapacityChangePreview {
    /** People at the front of the queue who will be sent an offer straight away. */
    offers: number;
    /** Public seats left over after those offers. While the waitlist is active they open for self-service only once
     *  every offer has been claimed or has expired. */
    seatsOpening: number;
}

/**
 * Mirrors the backend's waitlist offer decision (`TicketCatalog.UpdateTicketType`, then
 * `ProcessWaitlistNotificationsHandler`): raising the public capacity of a ticket type in waitlist mode sends an
 * offer to the front of the queue for every public seat that isn't used or held by an outstanding offer. Admin
 * tickets come on top of the public capacity (ADR-019), so they don't reduce the number of offers.
 * Returns `null` when the change sends no offers.
 */
export function previewPublicCapacityChange(
    t: TicketTypeDto,
    newPublicCapacity: number | undefined,
): PublicCapacityChangePreview | null {
    const currentCapacity = t.publicCapacity == null ? null : Number(t.publicCapacity);
    if (newPublicCapacity === undefined || newPublicCapacity === currentCapacity) return null;
    if (!t.waitlistEnabled || !t.waitlistMode) return null;

    const available = newPublicCapacity
        - (Number(t.publicUsedCapacity) || 0)
        - (Number(t.waitlistHeldCapacity) || 0);
    const offers = Math.min(Math.max(0, available), Number(t.waitlistQueuedCount) || 0);
    if (offers === 0) return null;

    return { offers, seatsOpening: available - offers };
}

/** "1 person" / "6 people"-style count. */
export function countOf(count: number, singular: string, plural: string): string {
    return `${count} ${count === 1 ? singular : plural}`;
}
