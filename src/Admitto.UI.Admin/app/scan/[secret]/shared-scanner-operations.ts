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
 * The anonymous shared scanner's operation surface. Lookup is a stub — the shared
 * scanner does not yet support manual name/email lookup (tracked separately), so
 * `supportsLookup: false` hides that control instead of showing a silently broken
 * search box. Check-in retains full server-side authoritative validation.
 *
 * A 401 mid-session means the link was revoked, regenerated, or expired (or the
 * event became inactive) since the page loaded. Rather than surface that as a
 * retryable network error, reload the page so the session bootstrap re-runs and
 * renders the same neutral access-denied state as an initially invalid link.
 */
export function createSharedScannerCheckInOperations(secret: string): CheckInOperations {
    return {
        checkIn: async (credential) => {
            const res = await fetch(`/api/scan/${secret}/check-in`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ credential }),
            });

            if (res.status === 401) {
                window.location.reload();
                return new Promise<CheckInResponse>(() => {});
            }

            if (!res.ok) throw new Error(`Request failed with status ${res.status}`);

            return (await res.json()) as CheckInResponse;
        },
        lookup: (): Promise<CheckInLookupCandidateDto[]> => Promise.resolve([]),
        supportsLookup: false,
    };
}
