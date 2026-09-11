import { act, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CheckInScanner, type DecoderAdapter } from "./scanner";
import { renderWithProviders } from "@/test-utils/render";
import { apiClient } from "@/lib/api-client";

vi.mock("@/lib/api-client", () => ({ apiClient: { post: vi.fn(), get: vi.fn() } }));
const post = vi.mocked(apiClient.post);

function fakeDecoder() {
    let decode: ((value: string) => void) | undefined;
    const decoder: DecoderAdapter = { start: vi.fn(async (callback) => { decode = callback; }), stop: vi.fn(async () => undefined) };
    return { decoder, scan: (value: string) => decode?.(value) };
}

describe("check-in scanner", () => {
    beforeEach(() => { post.mockResolvedValue({ outcome: "success", registrationId: "r1", name: "Jane Doe", ticketSelections: [{ id: "t1", name: "General" }], checkedInAt: "2027-06-12T18:01:00Z" }); });
    afterEach(() => vi.restoreAllMocks());

    // Given a fake decoder emits a credential
    // When the scanner receives the decoded value
    // Then it immediately dispatches one check-in request and shows the attendee and ticket
    it("dispatches decoded values and resets the success state", async () => {
        vi.useFakeTimers();
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner teamId="team" eventId="event" startsAt="2027-06-12T18:00:00Z" timeZone="Europe/Amsterdam" decoder={fake.decoder} />);
        await act(async () => fake.scan("credential-1"));
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "credential-1" });
        expect(screen.getByText(/Jane Doe · General/)).toBeInTheDocument();
        act(() => vi.advanceTimersByTime(2000));
        expect(screen.queryByText(/Jane Doe · General/)).not.toBeInTheDocument();
        vi.useRealTimers();
    });

    // Given a keyboard wedge sends an Enter key outside an editable field
    // When the scanner receives the wedge payload
    // Then it submits the credential without requiring manual confirmation
    it("accepts keyboard wedge input", async () => {
        renderWithProviders(<CheckInScanner teamId="team" eventId="event" startsAt="2027-06-12T18:00:00Z" timeZone="UTC" decoder={fakeDecoder().decoder} />);
        await act(async () => { for (const key of ["w", "e", "d", "g", "e", "Enter"]) window.dispatchEvent(new KeyboardEvent("keydown", { key })); });
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "wedge" });
    });
});
