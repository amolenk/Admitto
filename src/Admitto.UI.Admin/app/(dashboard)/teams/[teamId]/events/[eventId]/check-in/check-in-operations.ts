import type { QueryClient } from "@tanstack/react-query";
import type { CheckInLookupCandidateDto, CheckInResponse } from "@/lib/admitto-api/generated/types.gen";
import { apiClient } from "@/lib/api-client";

/**
 * The scan-operation surface the scanner needs from its access context.
 *
 * The signed-in dashboard and any other access context (for example, an
 * event-scoped shared scanner) each supply their own implementation, so the
 * scanner itself never depends on dashboard-only BFF routes or navigation.
 */
export type CheckInOperations = {
    checkIn: (credential: string) => Promise<CheckInResponse>;
    lookup: (query: string) => Promise<CheckInLookupCandidateDto[]>;
    /** Optional: notify the caller after a successful check-in (for example, to refresh a dashboard summary). */
    onCheckedIn?: (response: CheckInResponse) => void;
    /** Optional: link shown on a cancelled outcome. Omit to hide dashboard administration from the scanner. */
    createRegistrationHref?: string;
    /** Optional: hide manual name/email search when the access context doesn't support lookup yet. Defaults to true. */
    supportsLookup?: boolean;
};

/** The current signed-in crew scanner's operation surface, unchanged from prior behavior. */
export function createDashboardCheckInOperations(
    teamId: string,
    eventId: string,
    queryClient: QueryClient,
): CheckInOperations {
    return {
        checkIn: (credential) =>
            apiClient.post<CheckInResponse>(
                `/api/teams/${teamId}/events/${eventId}/registrations/check-in`,
                { credential },
            ),
        lookup: (query) =>
            apiClient.get<CheckInLookupCandidateDto[]>(
                `/api/teams/${teamId}/events/${eventId}/registrations/check-in/lookup?query=${encodeURIComponent(query)}`,
            ),
        onCheckedIn: () => {
            void queryClient.invalidateQueries({ queryKey: ["check-in-summary", teamId, eventId] });
        },
        createRegistrationHref: `/teams/${teamId}/events/${eventId}/registrations?create=1`,
    };
}
