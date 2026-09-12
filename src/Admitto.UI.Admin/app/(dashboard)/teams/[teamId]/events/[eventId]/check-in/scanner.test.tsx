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
    let queued: string | undefined;
    const decoder: DecoderAdapter = { start: vi.fn(async (callback) => { decode = callback; if (queued) { callback(queued); queued = undefined; } }), stop: vi.fn(async () => undefined) };
    return { decoder, scan: (value: string) => { if (decode) decode(value); else queued = value; } };
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
    it("submit_successfulCredential_showsAttendeeAndTicket", async () => {
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} timeZone="Europe/Amsterdam" decoder={fake.decoder} />);
        await act(async () => fake.scan("credential-1"));
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "credential-1" });
        expect(screen.getByText(/Jane Doe · General/)).toBeInTheDocument();
        expect(screen.getByText("Ready for the next scan")).toBeInTheDocument();
    });

    // Given the scanner page is open before the early-arrival window
    // When the 30-minute threshold passes
    // Then the warning appears without interrupting scanning
    it("clock_thresholdArrives_showsEarlyArrivalWarning", async () => {
        vi.useFakeTimers();
        const startsAt = new Date(2030, 0, 1, 10, 0).toISOString();
        vi.setSystemTime(new Date(new Date(startsAt).getTime() - 30 * 60_000 - 1000));
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} startsAt={startsAt} decoder={fake.decoder} />);

        expect(screen.queryByText(/Event starts at/)).not.toBeInTheDocument();
        await act(async () => { vi.advanceTimersByTime(1000); });
        expect(screen.getByText(/Event starts at/)).toBeInTheDocument();

        await act(async () => fake.scan("early-arrival"));
        expect(post).toHaveBeenCalledWith(expect.stringContaining("/check-in"), { credential: "early-arrival" });
        vi.useRealTimers();
    });

    // Given the scanner has paused after a successful scan
    // When two seconds elapse
    // Then the result clears and a later camera callback is accepted
    it("timer_twoSecondsElapse_resumesCameraScanning", async () => {
        vi.useFakeTimers();
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("first"));
        expect(screen.getByText(/Jane Doe · General/)).toBeInTheDocument();
        await act(async () => { vi.advanceTimersByTime(2000); });
        expect(screen.queryByText(/Jane Doe · General/)).not.toBeInTheDocument();
        await act(async () => fake.scan("second"));

        expect(post).toHaveBeenCalledTimes(2);
        expect(post).toHaveBeenLastCalledWith(expect.stringContaining("/check-in"), { credential: "second" });
        vi.useRealTimers();
    });

    // Given the camera reports the same visible QR code more than once
    // When the first check-in succeeds
    // Then the credential is suppressed until the success state is reset
    it("submit_duplicateDecoderValue_doesNotResubmitWhileSuccessVisible", async () => {
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("same-credential"));
        await act(async () => fake.scan("same-credential"));

        expect(post).toHaveBeenCalledTimes(1);
    });

    // Given a keyboard wedge sends an Enter key outside an editable field
    // When the scanner receives the wedge payload
    // Then it submits the credential without requiring manual confirmation
    it("keydown_wedgePayloadArrives_submitsCredential", async () => {
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
    ] as const)("submit_%s_outcomeRemainsUntilDismissed", async (outcome, message) => {
        post.mockResolvedValueOnce(response(outcome));
        const fake = fakeDecoder();
        const { user } = renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("credential-outcome"));
        expect(screen.getByRole("alert")).toHaveTextContent(message);
        expect(screen.getByRole("button", { name: "Dismiss" })).toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Dismiss" }));
        expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });

    // Given a terminal non-success outcome is displayed
    // When another camera value and keyboard-wedge value arrive
    // Then neither replaces the outcome until it is dismissed
    it("input_terminalOutcomeBlocksDecoderAndWedgeUntilDismissed", async () => {
        post.mockResolvedValueOnce(response("alreadyCheckedIn")).mockResolvedValueOnce(response("success"));
        const fake = fakeDecoder();
        const { user } = renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("duplicate"));
        await act(async () => fake.scan("ignored-camera"));
        await act(async () => { for (const key of ["i", "g", "n", "o", "r", "e", "d", "Enter"]) window.dispatchEvent(new KeyboardEvent("keydown", { key })); });
        expect(post).toHaveBeenCalledTimes(1);
        expect(screen.getByText(/Already checked in/)).toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Dismiss" }));
        await act(async () => fake.scan("accepted-after-dismiss"));
        expect(post).toHaveBeenCalledTimes(2);
    });

    // Given a cancelled registration is scanned
    // When the cancelled outcome is displayed
    // Then the operator can open the existing registration workflow
    it("submit_cancelledOutcome_linksToCreateRegistration", async () => {
        post.mockResolvedValueOnce(response("cancelled"));
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} decoder={fake.decoder} />);

        await act(async () => fake.scan("cancelled-credential"));

        expect(screen.getByRole("link", { name: "Create registration" })).toHaveAttribute(
            "href",
            "/teams/team/events/event/registrations?create=1",
        );
    });

    // Given the scanner is open with an attendance summary
    // When a check-in succeeds
    // Then it does not expose attendance metrics in the scanner header
    it("submit_successfulCheckIn_hidesAttendanceMetrics", async () => {
        const fake = fakeDecoder();
        renderWithProviders(<CheckInScanner {...props} summary={{ checkedInCount: 2, expectedCount: 4 }} decoder={fake.decoder} />);

        expect(screen.queryByText(/checked in · 4 expected/)).not.toBeInTheDocument();
        await act(async () => fake.scan("counted-credential"));
        expect(screen.queryByText("Authoritative count")).not.toBeInTheDocument();
    });

    // Given a matching registration returned by manual lookup
    // When the operator selects it and confirms check-in
    // Then the selected registration credential is submitted
    it("lookup_eligibleCandidate_confirmSubmitsRegistration", async () => {
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
    it("submit_networkFailure_retryResubmitsRetainedCredential", async () => {
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
    it("submit_successfulCheckIn_refetchesAuthoritativeSummary", async () => {
        let authoritativeCount = 2;
        get.mockImplementation((url: string) => {
            if (url === SUMMARY_URL) {
                return Promise.resolve({ checkedInCount: authoritativeCount, expectedCount: 4 });
            }
            return Promise.resolve([]);
        });
        const fake = fakeDecoder();
        renderWithProviders(<SummaryHarness decoder={fake.decoder} />);
        expect(await screen.findByRole("heading", { name: "Scan a ticket" })).toBeInTheDocument();

        authoritativeCount = 3;
        await act(async () => fake.scan("new-credential"));
        expect(get.mock.calls.filter(([url]) => url === SUMMARY_URL).length).toBeGreaterThanOrEqual(2);
    });
});
