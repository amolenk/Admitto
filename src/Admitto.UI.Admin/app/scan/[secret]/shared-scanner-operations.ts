import type { CheckInLookupCandidateDto, CheckInResponse, SharedScannerSessionDto } from "@/lib/admitto-api/generated/types.gen";
import type { CheckInOperations } from "../../(dashboard)/teams/[teamId]/events/[eventId]/check-in/check-in-operations";

export class SharedScannerAccessDeniedError extends Error {}

const ACCESS_DENIED_MESSAGE = "This scanner link is no longer valid. Contact the event organizer for access.";

export function fetchSharedScannerSession(secret: string): Promise<SharedScannerSessionDto> {
    return fetch(`/api/scan/${secret}`).then(async (res) => {
        if (res.status === 401) throw new SharedScannerAccessDeniedError(ACCESS_DENIED_MESSAGE);
        if (!res.ok) throw new Error(`Request failed with status ${res.status}`);
        return (await res.json()) as SharedScannerSessionDto;
    });
}

/**
 * A 401 mid-session means the link was revoked, regenerated, or expired (or the
 * event became inactive) since the page loaded. Rather than surface that as a
 * retryable network error, reload the page so the session bootstrap re-runs and
 * renders the same neutral access-denied state as an initially invalid link.
 */
function reloadOnInvalidatedSession(): never {
    window.location.reload();
    return new Promise<never>(() => {}) as never;
}

async function fetchOrReloadOnInvalidatedSession<T>(input: string, init?: RequestInit): Promise<T> {
    const res = await fetch(input, init);

    if (res.status === 401) return reloadOnInvalidatedSession();
    if (!res.ok) throw new Error(`Request failed with status ${res.status}`);

    return (await res.json()) as T;
}

/**
 * The anonymous shared scanner's operation surface. Both check-in and manual
 * name/email lookup are event-scoped through the shared-scanner secret and
 * retain full server-side authoritative validation.
 */
export function createSharedScannerCheckInOperations(secret: string): CheckInOperations {
    return {
        checkIn: (credential) =>
            fetchOrReloadOnInvalidatedSession<CheckInResponse>(`/api/scan/${secret}/check-in`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ credential }),
            }),
        lookup: (query) =>
            fetchOrReloadOnInvalidatedSession<CheckInLookupCandidateDto[]>(
                `/api/scan/${secret}/lookup?query=${encodeURIComponent(query)}`,
            ),
    };
}
