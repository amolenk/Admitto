import { screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";
import SharedScannerPage from "./page";

const fetchMock = vi.fn();

describe("shared scanner page", () => {
    afterEach(() => {
        vi.unstubAllGlobals();
        vi.clearAllMocks();
    });

    // Given a valid shared scanner secret
    // When the page loads
    // Then it bootstraps the session and renders the scanner without any sign-in
    it("validSecret_rendersScannerWithoutSignIn", async () => {
        setRoute({ params: { secret: "valid-secret" } });
        fetchMock.mockResolvedValue({
            status: 200,
            ok: true,
            json: () => Promise.resolve({
                teamId: "team-1",
                eventId: "event-1",
                eventName: "Conference",
                startsAt: new Date(Date.now() + 3_600_000).toISOString(),
                timeZone: "UTC",
            }),
        });
        vi.stubGlobal("fetch", fetchMock);

        renderWithProviders(<SharedScannerPage />);

        await screen.findByText("Scan a ticket");
        expect(fetchMock).toHaveBeenCalledWith("/api/scan/valid-secret");
    });

    // Given an expired, revoked, or malformed shared scanner secret
    // When the page loads
    // Then it shows a neutral access-denied message instead of the scanner
    it("invalidSecret_showsNeutralAccessDeniedMessage", async () => {
        setRoute({ params: { secret: "invalid-secret" } });
        fetchMock.mockResolvedValue({ status: 401, ok: false, json: () => Promise.resolve(null) });
        vi.stubGlobal("fetch", fetchMock);

        renderWithProviders(<SharedScannerPage />);

        await screen.findByText("Scanner link unavailable");
        expect(screen.queryByText("Scan a ticket")).not.toBeInTheDocument();
    });
});
