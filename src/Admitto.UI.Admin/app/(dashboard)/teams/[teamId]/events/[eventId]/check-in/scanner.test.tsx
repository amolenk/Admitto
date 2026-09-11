import { act, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useQuery } from "@tanstack/react-query";
import { CheckInScanner, type DecoderAdapter } from "./scanner";
import { renderWithProviders } from "@/test-utils/render";
import { apiClient } from "@/lib/api-client";

vi.mock("@/lib/api-client", () => ({ apiClient: { post: vi.fn(), get: vi.fn() } }));
const post = vi.mocked(apiClient.post);
const get = vi.mocked(apiClient.get);

const props = { teamId: "team", eventId: "event", startsAt: "2027-06-12T18:00:00Z", timeZone: "UTC" };
const SUMMARY_URL = "/api/teams/team/events/event/registrations/check-in/summary";

function response(outcome: "success" | "alreadyCheckedIn" | "cancelled" | "invalidForEvent" | "eventNotActive") {
    return { outcome, registrationId: "r1", name: "Jane Doe", ticketSelections: [{ id: "t1", name: "General" }], checkedInAt: "2027-06-12T18:01:00Z" };
}

function fakeDecoder() {
    let decode: ((value: string) => void) | undefined;
    const decoder: DecoderAdapter = { start: vi.fn(async (callback) => { decode = callback; }), stop: vi.fn(async () => undefined) };
    return { decoder, scan: (value: string) => decode?.(value) };
}

function SummaryHarness({ decoder }: { decoder: DecoderAdapter }) {
    const summary = useQuery({
        queryKey: ["check-in-summary", "team", "event"],
        queryFn: () => apiClient.get<{ checkedInCount: number; expectedCount: number }>(SUMMARY_URL),
    });

    return <CheckInScanner {...props} decoder={decoder} summary={summary.data} />;
}

describe("check-in scanner", () => {
    beforeEach(() => { post.mockResolvedValue(response("success")); get.mockResolvedValue([]); });
    afterEach(() => vi.restoreAllMocks());

    // Given a fake decoder emits a credential
    // When the scanner receives the decoded value
    // Then it immediately dispatches one check-in request and shows the attendee and ticket
    it("dispatches decoded values and keeps success visible until the operator resumes", async () => {
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} timeZone="Europe/Amsterdam" decoder={fake.decoder} />);
        await act(async () => fake.scan("credential-1"));
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "credential-1" });
        expect(screen.getByText(/Jane Doe · General/)).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Scan next" })).toBeInTheDocument();
    });

    // Given the scanner has paused after a successful scan
    // When the operator selects Scan next
    // Then a later camera callback is accepted
    it("resumes camera scanning only after an explicit Scan next action", async () => {
        const fake = fakeDecoder();
        const { user } = renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("first"));
        await user.click(screen.getByRole("button", { name: "Scan next" }));
        await act(async () => fake.scan("second"));

        expect(post).toHaveBeenCalledTimes(2);
        expect(post).toHaveBeenLastCalledWith(expect.stringContaining("/check-in"), { credential: "second" });
    });

    // Given a successful result is visible
    // When unrelated timer work runs
    // Then no stale timeout clears or replaces the current result
    it("does not use a stale success timer to clear the visible result", async () => {
        vi.useFakeTimers();
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);
        await act(async () => fake.scan("stable"));
        act(() => vi.advanceTimersByTime(10_000));
        expect(screen.getByText(/Jane Doe · General/)).toBeInTheDocument();
        vi.useRealTimers();
    });

    // Given the camera reports the same visible QR code more than once
    // When the first check-in succeeds
    // Then the credential is suppressed until the success state is reset
    it("does not resubmit a successful credential while it remains visible", async () => {
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("same-credential"));
        await act(async () => fake.scan("same-credential"));

        expect(post).toHaveBeenCalledTimes(1);
    });

    // Given a keyboard wedge sends an Enter key outside an editable field
    // When the scanner receives the wedge payload
    // Then it submits the credential without requiring manual confirmation
    it("accepts keyboard wedge input", async () => {
        renderWithProviders(<CheckInScanner {...props} decoder={fakeDecoder().decoder} />);
        await act(async () => { for (const key of ["w", "e", "d", "g", "e", "Enter"]) window.dispatchEvent(new KeyboardEvent("keydown", { key })); });
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "wedge" });
    });

    // Given a check-in response that is not successful
    // When the credential is scanned
    // Then the outcome remains visible until the operator dismisses it
    it.each([
        ["alreadyCheckedIn", /Already checked in/],
        ["cancelled", /Cancelled/],
        ["invalidForEvent", /not valid for this event/],
        ["eventNotActive", /not active/],
    ] as const)("retains the %s outcome until dismissal", async (outcome, message) => {
        post.mockResolvedValueOnce(response(outcome));
        const fake = fakeDecoder();
        const { user } = renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("credential-outcome"));
        expect(screen.getByRole("alert")).toHaveTextContent(message);
        expect(screen.getByRole("button", { name: "Dismiss" })).toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Dismiss" }));
        expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });

    // Given a matching registration returned by manual lookup
    // When the operator selects it and confirms check-in
    // Then the selected registration credential is submitted
    it("looks up a registration and confirms the selected candidate", async () => {
        get.mockResolvedValueOnce([{ registrationId: "r-manual", name: "Jane Doe", email: "jane@example.com", state: "eligible", checkedInAt: null }]);
        const { user } = renderWithProviders(<CheckInScanner {...props} decoder={fakeDecoder().decoder} />);
        await user.type(screen.getByRole("textbox", { name: "Manual search" }), "jane");
        await waitFor(() => expect(get).toHaveBeenCalledWith(expect.stringContaining("query=jane")));
        await user.click(screen.getByText("Jane Doe"));
        expect(screen.getByText("Check in Jane Doe?")).toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "Confirm check-in" }));
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "r-manual" });
    });

    // Given a selected registration and a temporary network failure
    // When the operator retries the check-in
    // Then the retained selection and credential are submitted successfully
    it("retains the manual selection for a network retry", async () => {
        get.mockResolvedValueOnce([{ registrationId: "r-retry", name: "Retry Person", email: "retry@example.com", state: "eligible", checkedInAt: null }]);
        post.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(response("success"));
        const { user } = renderWithProviders(<CheckInScanner {...props} decoder={fakeDecoder().decoder} />);
        await user.type(screen.getByRole("textbox", { name: "Manual search" }), "retry");
        await waitFor(() => expect(get).toHaveBeenCalled());
        await user.click(screen.getByText("Retry Person"));
        await user.click(screen.getByRole("button", { name: "Confirm check-in" }));
        expect(screen.getByText(/credential is retained/)).toBeInTheDocument();
        expect(screen.getByText("Check in Retry Person?")).toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "Retry" }));
        await waitFor(() => expect(post).toHaveBeenCalledTimes(2));
        expect(post).toHaveBeenLastCalledWith(expect.stringContaining("/check-in"), { credential: "r-retry" });
    });

    // Given the scanner is displaying the server's check-in summary
    // When a check-in succeeds and the authoritative summary changes
    // Then the scanner invalidates/refetches that query and renders the new values
    it("refetches and renders updated authoritative summary values after success", async () => {
        let authoritativeCount = 2;
        get.mockImplementation((url: string) => {
            if (url === SUMMARY_URL) {
                return Promise.resolve({ checkedInCount: authoritativeCount, expectedCount: 4 });
            }
            return Promise.resolve([]);
        });
        const fake = fakeDecoder();
        renderWithProviders(<SummaryHarness decoder={fake.decoder} />);
        expect(await screen.findByText("2 checked in · 4 expected · 50%")).toBeInTheDocument();

        authoritativeCount = 3;
        await act(async () => fake.scan("new-credential"));
        expect(await screen.findByText("3 checked in · 4 expected · 75%")).toBeInTheDocument();
        expect(get.mock.calls.filter(([url]) => url === SUMMARY_URL).length).toBeGreaterThanOrEqual(2);
    });
});
