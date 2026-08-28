import type {
    AdditionalDetailFieldDto,
    ApiKeyListItemDto,
    BulkEmailJobDetailDto,
    BulkEmailListItemDto,
    HttpValidationProblemDetails,
    PendingNotificationRow,
    RegistrationListItemDto,
    TeamDto,
    TeamListItemDto,
    TeamMemberListItemDto,
    TeamMembershipRoleDto,
    TicketedEventDetailsDto,
    TicketedEventListItemDto,
    TicketTypeDto,
    WaitlistDetailsDto,
    WaitlistEntryRow,
} from "@/lib/admitto-api/generated";

/**
 * Test data builders, following the backend convention in `tests/Admitto.Testing/Builders/`:
 * a valid default plus shallow overrides, so a test states only what it cares about.
 *
 * Builders are typed against `generated/types.gen.ts` on purpose — when the OpenAPI spec is
 * regenerated and a contract changes, they stop compiling instead of silently drifting.
 *
 * Add DTO builders (`ticketTypeDto`, `teamListItemDto`, …) here as tests need them, rather
 * than up front.
 */

/** A team as `/api/teams` lists it, with full permissions unless overridden. */
export function teamListItemDto(overrides: Partial<TeamListItemDto> = {}): TeamListItemDto {
    return {
        teamId: "11111111-1111-1111-1111-111111111111",
        name: "Contoso Crew",
        accentColor: "#3b82f6",
        version: 1,
        canManageTeamSettings: true,
        canCreateEvents: true,
        ...overrides,
    };
}

/** A team as `/api/teams/{teamId}` returns it. */
export function teamDto(overrides: Partial<TeamDto> = {}): TeamDto {
    return {
        teamId: "11111111-1111-1111-1111-111111111111",
        name: "Contoso Crew",
        accentColor: "#3b82f6",
        version: 1,
        ...overrides,
    };
}

/** A team member. Defaults to the least-privileged role, so tests opt into power. */
export function teamMemberDto(
    email: string,
    role: TeamMembershipRoleDto = "crew",
): TeamMemberListItemDto {
    return { email, role };
}

/** An API key. Active unless `revokedAt` is supplied. */
export function apiKeyDto(overrides: Partial<ApiKeyListItemDto> = {}): ApiKeyListItemDto {
    return {
        id: "aaaaaaaa-0000-0000-0000-000000000001",
        name: "Production",
        keyPrefix: "adm_live_abcd",
        createdAt: "2026-03-01T10:00:00Z",
        createdBy: "owner@example.com",
        revokedAt: null,
        ...overrides,
    };
}

/**
 * A ProblemDetails payload shaped like the backend's validation failures, for exercising the
 * `FormError` -> `useCustomForm` field-error mapping that every form relies on.
 */
export function validationProblemDetails(
    errors: Record<string, string[]>,
    overrides: Partial<HttpValidationProblemDetails> = {},
): HttpValidationProblemDetails {
    return {
        type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        title: "One or more validation errors occurred.",
        status: 400,
        detail: "See the errors property for details.",
        errors,
        ...overrides,
    };
}

/** A ticket type as `/api/teams/{teamId}/events/{eventId}/ticket-types` lists it. */
export function ticketTypeDto(overrides: Partial<TicketTypeDto> = {}): TicketTypeDto {
    return {
        id: "cccccccc-0000-0000-0000-000000000001",
        name: "General Admission",
        timeSlots: [],
        maxCapacity: null,
        usedCapacity: 0,
        selfServiceEnabled: true,
        waitlistEnabled: false,
        waitlistMode: false,
        claimWindowHours: 0,
        maxReconfirmationEmails: null,
        ...overrides,
    };
}

/** A registration as `/api/teams/{teamId}/events/{eventId}/registrations` lists it. */
export function registrationListItemDto(
    overrides: Partial<RegistrationListItemDto> = {},
): RegistrationListItemDto {
    return {
        id: "dddddddd-0000-0000-0000-000000000001",
        email: "jane.doe@example.com",
        firstName: "Jane",
        lastName: "Doe",
        tickets: [{ id: "cccccccc-0000-0000-0000-000000000001", name: "General Admission" }],
        additionalDetails: {},
        createdAt: "2026-03-01T10:00:00Z",
        status: "registered",
        hasReconfirmed: false,
        reconfirmedAt: null,
        ...overrides,
    };
}

/** A field in an event's `AdditionalDetailSchema`. */
export function additionalDetailFieldDto(
    overrides: Partial<AdditionalDetailFieldDto> = {},
): AdditionalDetailFieldDto {
    return {
        key: "dietary",
        name: "Dietary restrictions",
        maxLength: 20,
        ...overrides,
    };
}

/** A ticketed event as `/api/teams/{teamId}/events/{eventId}` returns it. */
export function ticketedEventDetailsDto(
    overrides: Partial<TicketedEventDetailsDto> = {},
): TicketedEventDetailsDto {
    return {
        id: "33333333-3333-3333-3333-333333333333",
        teamId: "11111111-1111-1111-1111-111111111111",
        name: "DevConf 2026",
        websiteUrl: "https://example.com",
        baseUrl: "https://example.com",
        publicSlug: "devconf-2026",
        startsAt: "2026-06-01T09:00:00Z",
        endsAt: "2026-06-02T18:00:00Z",
        timeZone: "Europe/Amsterdam",
        status: "active",
        version: 1,
        isRegistrationOpen: true,
        registrationPolicy: null,
        reconfirmPolicy: null,
        waitlistPolicy: { quietHoursStart: "22:00", quietHoursEnd: "08:00" },
        additionalDetailSchema: [],
        ...overrides,
    };
}

/** A ticketed event as `/api/teams/{teamId}/events` lists it. */
export function ticketedEventListItemDto(
    overrides: Partial<TicketedEventListItemDto> = {},
): TicketedEventListItemDto {
    return {
        id: "33333333-3333-3333-3333-333333333333",
        name: "DevConf 2026",
        publicSlug: "devconf-2026",
        startsAt: "2026-06-01T09:00:00Z",
        endsAt: "2026-06-02T18:00:00Z",
        timeZone: "Europe/Amsterdam",
        status: "active",
        ...overrides,
    };
}

/** A bulk email job as `/api/teams/{teamId}/events/{eventId}/bulk-emails` lists it. */
export function bulkEmailListItemDto(
    overrides: Partial<BulkEmailListItemDto> = {},
): BulkEmailListItemDto {
    return {
        id: "ffffffff-0000-0000-0000-000000000001",
        emailType: "bulk-custom",
        status: "completed",
        recipientCount: 10,
        sentCount: 10,
        failedCount: 0,
        cancelledCount: 0,
        isSystemTriggered: false,
        triggeredBy: "owner@example.com",
        createdAt: "2026-03-01T10:00:00Z",
        startedAt: "2026-03-01T10:00:05Z",
        completedAt: "2026-03-01T10:01:00Z",
        cancellationRequestedAt: null,
        cancelledAt: null,
        ...overrides,
    };
}

/** A bulk email job as `/api/teams/{teamId}/events/{eventId}/bulk-emails/{jobId}` returns it. */
export function bulkEmailJobDetailDto(
    overrides: Partial<BulkEmailJobDetailDto> = {},
): BulkEmailJobDetailDto {
    return {
        id: "ffffffff-0000-0000-0000-000000000001",
        teamId: "11111111-1111-1111-1111-111111111111",
        ticketedEventId: "33333333-3333-3333-3333-333333333333",
        emailType: "bulk-custom",
        subject: "Important update for DevConf 2026",
        textBody: "Hello, this is an important update.",
        htmlBody: "<p>Hello, this is an important update.</p>",
        attendeeFilter: {},
        status: "completed",
        recipientCount: 10,
        sentCount: 10,
        failedCount: 0,
        cancelledCount: 0,
        lastError: null,
        isSystemTriggered: false,
        triggeredBy: "owner@example.com",
        createdAt: "2026-03-01T10:00:00Z",
        startedAt: "2026-03-01T10:00:05Z",
        completedAt: "2026-03-01T10:01:00Z",
        cancellationRequestedAt: null,
        cancelledAt: null,
        version: 1,
        recipients: [],
        ...overrides,
    };
}

// ── Attendee-detail DTOs ─────────────────────────────────────────────────────
//
// These back the local interfaces declared in
// `(dashboard)/teams/[teamId]/events/[eventId]/registrations/[registrationId]/page.tsx`.
// They are not (yet) part of the generated SDK, so the shapes are duplicated here rather
// than imported — keep them in sync with the page if it changes.

export interface RegistrationDetailDto {
    id: string;
    email: string;
    firstName?: string | null;
    lastName?: string | null;
    status: string;
    registeredAt: string;
    hasReconfirmed: boolean;
    reconfirmedAt?: string | null;
    cancellationReason?: string | null;
    tickets: { id: string; name: string }[];
    additionalDetails: Record<string, string>;
    activities: ActivityLogEntryDto[];
}

export interface ActivityLogEntryDto {
    activityType: string;
    occurredAt: string;
    metadata?: string | null;
}

export interface AttendeeEmailLogItemDto {
    id: string;
    subject: string;
    emailType: string;
    status: string;
    sentAt?: string | null;
    bulkEmailJobId?: string | null;
}

/** A registration as the attendee-detail page's detail endpoint returns it. */
export function registrationDetailDto(
    overrides: Partial<RegistrationDetailDto> = {},
): RegistrationDetailDto {
    return {
        id: "dddddddd-0000-0000-0000-000000000001",
        email: "jane.doe@example.com",
        firstName: "Jane",
        lastName: "Doe",
        status: "registered",
        registeredAt: "2026-03-01T10:00:00Z",
        hasReconfirmed: false,
        reconfirmedAt: null,
        cancellationReason: null,
        tickets: [{ id: "cccccccc-0000-0000-0000-000000000001", name: "General Admission" }],
        additionalDetails: {},
        activities: [{ activityType: "Registered", occurredAt: "2026-03-01T10:00:00Z" }],
        ...overrides,
    };
}

/** A single activity-log entry on a registration's timeline. */
export function activityLogEntryDto(
    overrides: Partial<ActivityLogEntryDto> = {},
): ActivityLogEntryDto {
    return {
        activityType: "Registered",
        occurredAt: "2026-03-01T10:00:00Z",
        metadata: null,
        ...overrides,
    };
}

/** An email sent to an attendee, as the attendee-emails endpoint lists it. */
export function attendeeEmailLogItemDto(
    overrides: Partial<AttendeeEmailLogItemDto> = {},
): AttendeeEmailLogItemDto {
    return {
        id: "eeeeeeee-0000-0000-0000-000000000001",
        subject: "Your ticket is confirmed",
        emailType: "TicketConfirmation",
        status: "Delivered",
        sentAt: "2026-03-01T10:05:00Z",
        bulkEmailJobId: null,
        ...overrides,
    };
}

/** An active entry on a ticket type's waitlist, with its email already masked. */
export function waitlistEntryRow(overrides: Partial<WaitlistEntryRow> = {}): WaitlistEntryRow {
    return {
        entryId: "11112222-0000-0000-0000-000000000001",
        position: 1,
        maskedEmail: "ali***@example.com",
        joinedAt: "2026-08-01T10:00:00Z",
        ...overrides,
    };
}

/** A pending claim notification on a ticket type's waitlist. */
export function pendingNotificationRow(
    overrides: Partial<PendingNotificationRow> = {},
): PendingNotificationRow {
    return {
        couponId: "22223333-0000-0000-0000-000000000001",
        maskedEmail: "bob***@example.com",
        expiresAt: "2026-08-01T15:00:00Z",
        ...overrides,
    };
}

/** The full response of `GET .../ticket-types/{ticketTypeId}/waitlist`. */
export function waitlistDetailsDto(overrides: Partial<WaitlistDetailsDto> = {}): WaitlistDetailsDto {
    return {
        activeEntries: [waitlistEntryRow()],
        pendingNotifications: [pendingNotificationRow()],
        stats: { totalWaiting: 1, totalPending: 1, sentToday: 0 },
        ...overrides,
    };
}
