import type { CheckInOutcome, CheckInResponse } from "@/lib/admitto-api/generated/types.gen";

export type CheckInResult =
    | { kind: "success"; response: CheckInResponse }
    | { kind: "duplicate"; response: CheckInResponse }
    | { kind: "cancelled"; response: CheckInResponse }
    | { kind: "inactive"; response: CheckInResponse }
    | { kind: "invalid"; response: CheckInResponse };

export function mapCheckInOutcome(response: CheckInResponse): CheckInResult {
    const kinds: Record<CheckInOutcome, CheckInResult["kind"]> = {
        success: "success",
        alreadyCheckedIn: "duplicate",
        cancelled: "cancelled",
        eventNotActive: "inactive",
        invalidForEvent: "invalid",
    };
    return { kind: kinds[response.outcome], response } as CheckInResult;
}
