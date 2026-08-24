import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { toast } from "sonner";

import type { TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import AttendeeDetailPage from "./page";

// Harvested from TESTING-BACKLOG.md `admin-ui-attendee-detail`. Per Drift D5, "change ticket
// types" and "resend ticket email" are real mutations with success toasts, not placeholders —
// tested as implemented rather than asserting a "Coming soon" state.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock("sonner", () => ({
    toast: { success: vi.fn(), error: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);
const put = vi.mocked(apiClient.put);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";
const REGISTRATION_ID = "33333333-3333-3333-3333-333333333333";

// ── Local shapes ──────────────────────────────────────────────────────────────
// The page defines these DTOs itself (not (yet) re-exported from the generated SDK for this
// shape), so the factories below mirror the page's own local interfaces rather than the
// generated types.

interface ActivityLogEntry {
    activityType: string;
    occurredAt: string;
    metadata?: string | null;
}

interface TicketDetail {
    id: string;
    name: string;
}

interface RegistrationDetail {
    id: string;
    email: string;
    firstName?: string | null;
    lastName?: string | null;
    status: "registered" | "cancelled";
    registeredAt: string;
    hasReconfirmed: boolean;
    reconfirmedAt?: string | null;
    cancellationReason?: string | null;
    tickets: TicketDetail[];
    additionalDetails: Record<string, string>;
    activities: ActivityLogEntry[];
}

interface AttendeeEmailLogItem {
    id: string;
    subject: string;
    emailType: string;
    status: string;
    sentAt?: string | null;
    bulkEmailJobId?: string | null;
}

function activityEntry(overrides: Partial<ActivityLogEntry> = {}): ActivityLogEntry {
    return {
        activityType: "Registered",
        occurredAt: "2026-08-10T09:00:00Z",
        metadata: null,
        ...overrides,
    };
}

function registrationDetail(overrides: Partial<RegistrationDetail> = {}): RegistrationDetail {
    return {
        id: REGISTRATION_ID,
        email: "jane.doe@example.com",
        firstName: "Jane",
        lastName: "Doe",
        status: "registered",
        registeredAt: "2026-08-10T09:00:00Z",
        hasReconfirmed: false,
        reconfirmedAt: null,
        cancellationReason: null,
        tickets: [{ id: "tt-1", name: "General Admission" }],
        additionalDetails: {},
        activities: [activityEntry()],
        ...overrides,
    };
}

function emailLogItem(overrides: Partial<AttendeeEmailLogItem> = {}): AttendeeEmailLogItem {
    return {
        id: "email-1",
        subject: "Your ticket is confirmed",
        emailType: "TicketConfirmation",
        status: "Sent",
        sentAt: "2026-08-10T09:05:00Z",
        bulkEmailJobId: null,
        ...overrides,
    };
}

function ticketTypeDto(overrides: Partial<TicketTypeDto> = {}): TicketTypeDto {
    return {
        id: "tt-1",
        name: "General Admission",
        timeSlots: [],
        maxCapacity: null,
        usedCapacity: 0,
        selfServiceEnabled: true,
        waitlistEnabled: false,
        waitlistMode: false,
        claimWindowHours: 0,
        maxReconfirmAttempts: null,
        ...overrides,
    };
}

/** Wires `apiClient.get` to answer the detail, emails and ticket-types queries by URL shape. */
function mockApi(
    options: {
        detail?: RegistrationDetail;
        emails?: AttendeeEmailLogItem[];
        ticketTypes?: TicketTypeDto[];
    } = {},
) {
    const detail = options.detail ?? registrationDetail();
    const emails = options.emails ?? [];
    const ticketTypes = options.ticketTypes ?? [ticketTypeDto()];

    get.mockImplementation((path: string) => {
        if (path.endsWith("/emails")) return Promise.resolve(emails);
        if (path.endsWith("/ticket-types")) return Promise.resolve(ticketTypes);
        return Promise.resolve(detail);
    });

    return { detail, emails, ticketTypes };
}

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID, registrationId: REGISTRATION_ID } });
    return renderWithProviders(<AttendeeDetailPage />);
}

/** All timeline entry titles, in DOM order (top to bottom = most-recent-first). */
function timelineTitles(): string[] {
    return screen.getAllByRole("listitem").map((li) => li.textContent ?? "");
}

describe("AttendeeDetailPage", () => {
    beforeEach(() => {
        post.mockResolvedValue(undefined);
        put.mockResolvedValue(undefined);
    });

    // Given a registered attendee with a first and last name
    // When the page loads
    // Then the hero shows their full name, registered status and email
    it("renders details for a registered attendee", async () => {
        mockApi({
            detail: registrationDetail({ firstName: "Jane", lastName: "Doe", email: "jane.doe@example.com" }),
        });

        renderPage();

        expect(await screen.findByRole("heading", { level: 1, name: "Jane Doe" })).toBeInTheDocument();
        expect(screen.getAllByText("Registered").length).toBeGreaterThan(0);
        expect(screen.getAllByText("jane.doe@example.com").length).toBeGreaterThan(0);
    });

    // Given an attendee with no first or last name on file
    // When the page renders their name
    // Then it falls back to the local part of their email address
    it("falls back to the email prefix when the attendee's name is absent", async () => {
        mockApi({
            detail: registrationDetail({ firstName: "", lastName: "", email: "att.only@example.com" }),
        });

        renderPage();

        expect(await screen.findByRole("heading", { level: 1, name: "att.only" })).toBeInTheDocument();
    });

    // Given the event's additional-details schema captured extra fields for this attendee
    // When the details card renders
    // Then each additional field is shown alongside the fixed attendee fields
    it("renders additional details when present", async () => {
        mockApi({ detail: registrationDetail({ additionalDetails: { Company: "Acme Inc" } }) });

        renderPage();

        expect(await screen.findByText("Company")).toBeInTheDocument();
        expect(screen.getByText("Acme Inc")).toBeInTheDocument();
    });

    // Given the attendee has no additional details recorded
    // When the details card renders
    // Then only the fixed attendee fields are shown, with no leftover additional-detail rows
    it("hides the additional-details rows when there are none", async () => {
        mockApi({ detail: registrationDetail({ additionalDetails: {} }) });

        renderPage();

        const dl = (await screen.findByText("Full name")).closest("dl");
        expect(dl).not.toBeNull();
        expect(dl!.querySelectorAll("dt")).toHaveLength(5);
    });

    // Given an attendee who registered and later reconfirmed
    // When the timeline renders
    // Then the reconfirmation milestone appears above the earlier registration milestone
    it("shows registration and reconfirmation milestones, most-recent-first", async () => {
        mockApi({
            detail: registrationDetail({
                hasReconfirmed: true,
                reconfirmedAt: "2026-08-11T10:00:00Z",
                activities: [
                    activityEntry({ activityType: "Registered", occurredAt: "2026-08-10T09:00:00Z" }),
                    activityEntry({ activityType: "Reconfirmed", occurredAt: "2026-08-11T10:00:00Z" }),
                ],
            }),
        });

        renderPage();

        await screen.findByText("Attendance reconfirmed");
        const titles = timelineTitles();
        const reconfirmedIndex = titles.findIndex((t) => t.includes("Attendance reconfirmed"));
        const registeredIndex = titles.findIndex((t) => t.includes("Started registration"));
        expect(reconfirmedIndex).toBeGreaterThanOrEqual(0);
        expect(registeredIndex).toBeGreaterThan(reconfirmedIndex);
    });

    // Given a cancelled registration with a recorded reason
    // When the page renders
    // Then the timeline surfaces the cancellation with its human-readable reason, and the
    // Cancel button is gone since there is nothing left to cancel
    it("shows a cancellation entry with its reason and hides the Cancel button", async () => {
        mockApi({
            detail: registrationDetail({
                status: "cancelled",
                cancellationReason: "AttendeeRequest",
                activities: [
                    activityEntry({ activityType: "Registered", occurredAt: "2026-08-10T09:00:00Z" }),
                    activityEntry({
                        activityType: "Cancelled",
                        occurredAt: "2026-08-12T09:00:00Z",
                        metadata: "AttendeeRequest",
                    }),
                ],
            }),
        });

        renderPage();

        expect(await screen.findByText("Registration cancelled (Attendee request)")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Cancel registration" })).not.toBeInTheDocument();
    });

    // Given both activity-log entries and sent emails exist for the attendee
    // When the "All" feed renders (the default tab)
    // Then the two sources are merged into a single timeline, ordered by timestamp
    it("interleaves emails with activity entries in a single feed", async () => {
        const email = emailLogItem({ subject: "Your ticket is confirmed", sentAt: "2026-08-10T09:05:00Z" });
        mockApi({
            detail: registrationDetail({
                activities: [activityEntry({ activityType: "Registered", occurredAt: "2026-08-10T09:00:00Z" })],
            }),
            emails: [email],
        });

        renderPage();

        await screen.findByText("Started registration");
        expect(screen.getByText(email.subject)).toBeInTheDocument();

        const titles = timelineTitles();
        const emailIndex = titles.findIndex((t) => t.includes(email.subject));
        const registeredIndex = titles.findIndex((t) => t.includes("Started registration"));
        // The email was sent after registration, so it sorts above it.
        expect(emailIndex).toBeGreaterThanOrEqual(0);
        expect(emailIndex).toBeLessThan(registeredIndex);
    });

    // Given a merged feed of activity entries and emails
    // When the attendee clicks the "Emails" tab
    // Then only the email entries remain visible
    it("filters the feed down to emails only on the Emails tab", async () => {
        const email = emailLogItem();
        mockApi({
            detail: registrationDetail({
                activities: [activityEntry({ activityType: "Registered" })],
            }),
            emails: [email],
        });

        const { user } = renderPage();
        await screen.findByText("Started registration");

        await user.click(screen.getByRole("button", { name: "Emails" }));

        expect(screen.queryByText("Started registration")).not.toBeInTheDocument();
        expect(screen.getByText(email.subject)).toBeInTheDocument();
    });

    // Given an attendee with no activity and no emails
    // When the feed renders
    // Then an empty-state message is shown instead of an empty list
    it("shows an empty state when there are no feed entries", async () => {
        mockApi({ detail: registrationDetail({ activities: [] }), emails: [] });

        renderPage();

        expect(await screen.findByText("Nothing here yet.")).toBeInTheDocument();
    });

    // Given the detail and emails queries have not resolved yet
    // When the page first renders
    // Then skeleton placeholders are shown instead of the real content
    it("shows skeleton placeholders before data arrives", () => {
        get.mockImplementation(() => new Promise(() => {}));

        const { container } = renderPage();

        expect(container.querySelectorAll('[data-slot="skeleton"]').length).toBeGreaterThan(0);
        expect(screen.queryByText("Details")).not.toBeInTheDocument();
    });

    // Given a registered attendee
    // When the organizer opens the cancel dialog
    // Then it offers only the two supported reasons, no request is sent yet, and confirming
    // is blocked until a reason is chosen
    it("opens a cancel confirmation dialog with a limited set of reasons and sends nothing before confirming", async () => {
        mockApi();
        const { user } = renderPage();

        await user.click(await screen.findByRole("button", { name: "Cancel registration" }));

        expect(await screen.findByRole("heading", { name: "Cancel registration" })).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Confirm cancellation" })).toBeDisabled();
        expect(post).not.toHaveBeenCalled();

        await user.click(screen.getByRole("combobox"));
        expect(await screen.findByRole("option", { name: "Attendee request" })).toBeInTheDocument();
        expect(screen.getByRole("option", { name: "Visa letter denied" })).toBeInTheDocument();
        expect(screen.queryByRole("option", { name: "Ticket types removed" })).not.toBeInTheDocument();

        expect(post).not.toHaveBeenCalled();
    });

    // Given the cancel dialog is open with a reason selected
    // When the organizer confirms
    // Then the cancel endpoint is called with that reason, a success toast is shown, and the
    // dialog closes
    it("calls the cancel endpoint once the cancellation is confirmed", async () => {
        mockApi();
        const { user } = renderPage();

        await user.click(await screen.findByRole("button", { name: "Cancel registration" }));
        await user.click(screen.getByRole("combobox"));
        await user.click(await screen.findByRole("option", { name: "Attendee request" }));
        await user.click(screen.getByRole("button", { name: "Confirm cancellation" }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/registrations/${REGISTRATION_ID}/cancel`,
                { reason: "AttendeeRequest" },
            ),
        );
        expect(toast.success).toHaveBeenCalledWith("Registration cancelled successfully.");
        expect(screen.queryByRole("heading", { name: "Cancel registration" })).not.toBeInTheDocument();
    });

    // Given the organizer opens "Change ticket types" and keeps the current selection
    // When they save
    // Then the tickets endpoint is called with the selected ticket type ids and a success
    // toast confirms the update (per Drift D5, this is a real mutation, not a placeholder)
    it("changes ticket types via a working mutation and shows a success toast", async () => {
        mockApi({ ticketTypes: [ticketTypeDto({ id: "tt-1", name: "General Admission" })] });
        const { user } = renderPage();

        await user.click(await screen.findByRole("button", { name: "Change" }));
        await screen.findByRole("checkbox");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/registrations/${REGISTRATION_ID}/tickets`,
                { ticketTypeIds: ["tt-1"] },
            ),
        );
        expect(toast.success).toHaveBeenCalledWith("Ticket types updated successfully.");
        expect(screen.queryByRole("heading", { name: "Change ticket types" })).not.toBeInTheDocument();
    });

    // Given the attendee detail page
    // When the organizer looks at the back link
    // Then it points back at the event's registrations list
    it("has a back link that returns to the registrations list", async () => {
        mockApi();
        renderPage();

        const link = await screen.findByRole("link", { name: "Registrations" });
        expect(link).toHaveAttribute("href", `/teams/${TEAM_ID}/events/${EVENT_ID}/registrations`);
    });
});
