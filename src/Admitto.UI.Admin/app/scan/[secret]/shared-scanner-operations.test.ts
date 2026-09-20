import { afterEach, describe, expect, it, vi } from "vitest";
import { createSharedScannerCheckInOperations, fetchSharedScannerSession, SharedScannerAccessDeniedError } from "./shared-scanner-operations";

const fetchMock = vi.fn();

describe("shared scanner operations", () => {
    afterEach(() => {
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    // Given a valid shared scanner secret
    // When the session is fetched
    // Then it returns the event context from the bootstrap endpoint
    it("fetchSharedScannerSession_validSecret_returnsSessionContext", async () => {
        fetchMock.mockResolvedValue({
            status: 200,
            ok: true,
            json: () => Promise.resolve({ teamId: "t1", eventId: "e1", eventName: "Conf", startsAt: "2027-01-01T00:00:00Z", timeZone: "UTC" }),
        });
        vi.stubGlobal("fetch", fetchMock);

        const session = await fetchSharedScannerSession("secret");

        expect(session.teamId).toBe("t1");
    });

    // Given an invalid shared scanner secret
    // When the session is fetched
    // Then it throws a neutral access-denied error
    it("fetchSharedScannerSession_unauthorized_throwsAccessDeniedError", async () => {
        fetchMock.mockResolvedValue({ status: 401, ok: false });
        vi.stubGlobal("fetch", fetchMock);

        await expect(fetchSharedScannerSession("secret")).rejects.toBeInstanceOf(SharedScannerAccessDeniedError);
    });

    // Given a scanner session whose link is later revoked, regenerated, or expired
    // When a check-in is attempted and the API returns Unauthorized
    // Then the page reloads instead of surfacing a misleading retryable network error
    it("checkIn_unauthorizedMidSession_reloadsPageInsteadOfResolving", async () => {
        fetchMock.mockResolvedValue({ status: 401, ok: false });
        vi.stubGlobal("fetch", fetchMock);
        const reload = vi.fn();
        vi.stubGlobal("location", { ...window.location, reload });

        const operations = createSharedScannerCheckInOperations("secret");
        void operations.checkIn("credential");
        await Promise.resolve();

        expect(reload).toHaveBeenCalled();
    });

    // Given a valid shared scanner link
    // When lookup is called with a search query
    // Then it returns the matching candidates from the shared-scanner lookup endpoint
    it("lookup_validLink_returnsCandidates", async () => {
        const candidate = {
            registrationId: "r1",
            name: "Alice Attendee",
            email: "alice@example.com",
            state: "eligible",
            checkedInAt: null,
        };
        fetchMock.mockResolvedValue({
            status: 200,
            ok: true,
            json: () => Promise.resolve([candidate]),
        });
        vi.stubGlobal("fetch", fetchMock);

        const operations = createSharedScannerCheckInOperations("secret");
        const candidates = await operations.lookup("Alice");

        expect(candidates).toEqual([candidate]);
        expect(fetchMock).toHaveBeenCalledWith("/api/scan/secret/lookup?query=Alice", undefined);
    });

    // Given a scanner session whose link is later revoked, regenerated, or expired
    // When lookup is attempted and the API returns Unauthorized
    // Then the page reloads instead of surfacing a misleading retryable network error
    it("lookup_unauthorizedMidSession_reloadsPageInsteadOfResolving", async () => {
        fetchMock.mockResolvedValue({ status: 401, ok: false });
        vi.stubGlobal("fetch", fetchMock);
        const reload = vi.fn();
        vi.stubGlobal("location", { ...window.location, reload });

        const operations = createSharedScannerCheckInOperations("secret");
        void operations.lookup("anything");
        await Promise.resolve();

        expect(reload).toHaveBeenCalled();
    });
});
