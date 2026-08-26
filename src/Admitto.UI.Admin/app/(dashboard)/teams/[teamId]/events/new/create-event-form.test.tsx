import { act, fireEvent, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";
import { routerMock, setRoute } from "@/test-utils/router";

import { CreateEventForm } from "./create-event-form";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

// The date/time controls have their own interaction tests. Keeping them as inputs here makes
// these tests focus on the create request and its asynchronous status workflow.
vi.mock("@/components/ui/time-zone-selector", () => ({
    TimeZoneSelector: ({
        value,
        onChange,
        onBlur,
        disabled,
    }: {
        value?: string;
        onChange?: (value: string) => void;
        onBlur?: () => void;
        disabled?: boolean;
    }) => (
        <input
            aria-label="Time zone"
            value={value ?? ""}
            onChange={(event) => onChange?.(event.target.value)}
            onBlur={onBlur}
            disabled={disabled}
        />
    ),
}));

vi.mock("@/components/ui/zoned-date-time-picker", () => ({
    ZonedDateTimePicker: ({
        value,
        onChange,
        onBlur,
        disabled,
    }: {
        value?: string;
        onChange?: (value: string) => void;
        onBlur?: () => void;
        disabled?: boolean;
    }) => (
        <input
            aria-label="Event date and time"
            value={value ?? ""}
            onChange={(event) => onChange?.(event.target.value)}
            onBlur={onBlur}
            disabled={disabled}
        />
    ),
}));

const post = vi.mocked(apiClient.post);
const get = vi.mocked(apiClient.get);

const TEAM_ID = "44444444-4444-4444-4444-444444444444";
const EVENT_ID = "55555555-5555-5555-5555-555555555555";
const CREATION_ID = "66666666-6666-6666-6666-666666666666";

function renderForm() {
    setRoute({ params: { teamId: TEAM_ID }, pathname: `/teams/${TEAM_ID}/events/new` });
    return renderWithProviders(<CreateEventForm />);
}

function fillValidForm() {
    fireEvent.change(screen.getByPlaceholderText("e.g. DevConf 2026"), {
        target: { value: "Admitto Summit" },
    });
    fireEvent.change(screen.getByPlaceholderText("azure-fest-2026"), {
        target: { value: "admitto-summit" },
    });
    fireEvent.change(screen.getByPlaceholderText("https://example.com"), {
        target: { value: "https://example.com/event" },
    });
    fireEvent.change(screen.getByPlaceholderText("https://register.example.com"), {
        target: { value: "https://register.example.com" },
    });
    fireEvent.change(screen.getByRole("textbox", { name: "Time zone" }), {
        target: { value: "UTC" },
    });

    const dateInputs = screen.getAllByRole("textbox", { name: "Event date and time" });
    fireEvent.change(dateInputs[0]!, { target: { value: "2026-06-01T09:00:00.000Z" } });
    fireEvent.change(dateInputs[1]!, { target: { value: "2026-06-02T18:00:00.000Z" } });
}

function acceptedResponse() {
    return { creationRequestId: CREATION_ID, statusUrl: `/status/${CREATION_ID}` };
}

function statusResponse(
    status: "Pending" | "Created" | "Rejected" | "Expired",
    overrides: Record<string, unknown> = {},
) {
    return {
        creationRequestId: CREATION_ID,
        teamId: TEAM_ID,
        requesterId: "77777777-7777-7777-7777-777777777777",
        requestedAt: "2026-05-01T10:00:00Z",
        status,
        ...overrides,
    };
}

describe("CreateEventForm", () => {
    beforeEach(() => {
        post.mockReset();
        get.mockReset();
        routerMock.push.mockReset();
    });

    afterEach(() => {
        vi.clearAllTimers();
        vi.useRealTimers();
    });

    it("blocks an invalid create and displays client validation", async () => {
        const { user } = renderForm();

        await user.click(screen.getByRole("button", { name: "Create event" }));

        expect(await screen.findByText("Name is required")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

});

describe("CreateEventForm polling", () => {
    beforeEach(() => {
        vi.useFakeTimers();
        post.mockReset();
        get.mockReset();
        post.mockResolvedValue(acceptedResponse());
    });

    afterEach(() => {
        vi.clearAllTimers();
        vi.useRealTimers();
    });

    async function submitAndAdvanceFirstPoll() {
        fireEvent.click(screen.getByRole("button", { name: "Create event" }));
        await act(async () => {
            await Promise.resolve();
            await Promise.resolve();
            await Promise.resolve();
        });
        expect(post).toHaveBeenCalled();
        await act(async () => {
            await vi.advanceTimersByTimeAsync(500);
        });
    }

    async function settleAsyncWork() {
        await act(async () => {
            await Promise.resolve();
            await Promise.resolve();
        });
    }

    async function expectFormReenabled() {
        await settleAsyncWork();
        expect(screen.getByRole("button", { name: "Create event" })).toBeEnabled();
        expect(screen.getByPlaceholderText("e.g. DevConf 2026")).toBeEnabled();
    }

    it("submits the exact request and lands on the event dashboard after creation", async () => {
        get.mockResolvedValue(
            statusResponse("Created", { ticketedEventId: EVENT_ID }),
        );
        renderForm();
        fillValidForm();

        await submitAndAdvanceFirstPoll();
        await settleAsyncWork();

        expect(post).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/events`, {
            name: "Admitto Summit",
            publicSlug: "admitto-summit",
            websiteUrl: "https://example.com/event",
            baseUrl: "https://register.example.com",
            timeZone: "UTC",
            startsAt: "2026-06-01T09:00:00.000Z",
            endsAt: "2026-06-02T18:00:00.000Z",
        });
        expect(get).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/event-creations/${CREATION_ID}`);
        await vi.waitFor(() =>
            expect(routerMock.push).toHaveBeenCalledWith(`/teams/${TEAM_ID}/events/${EVENT_ID}`),
        );
        expect(routerMock.push).not.toHaveBeenCalledWith(
            `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/general`,
        );
    });

    it("keeps the form busy and shows progress while status is Pending", async () => {
        get.mockResolvedValue(statusResponse("Pending"));
        renderForm();
        fillValidForm();

        await submitAndAdvanceFirstPoll();

        expect(screen.getByText("Creating your event…")).toBeInTheDocument();
        const progress = screen.getByRole("progressbar");
        expect(progress.querySelector('[data-slot="progress-indicator"]')).toHaveAttribute(
            "style",
            expect.stringMatching(/translateX\(-9\d%\)/),
        );
        expect(screen.getByRole("button", { name: /Creating…/ })).toBeDisabled();
        expect(screen.getByPlaceholderText("e.g. DevConf 2026")).toBeDisabled();
    });

    it("navigates when polling returns Created", async () => {
        get.mockResolvedValue(
            statusResponse("Created", { ticketedEventId: EVENT_ID }),
        );
        renderForm();
        fillValidForm();

        await submitAndAdvanceFirstPoll();

        expect(routerMock.push).toHaveBeenCalledWith(`/teams/${TEAM_ID}/events/${EVENT_ID}`);
        expect(screen.getByText("Opening event…")).toBeInTheDocument();
    });

    it("reports a duplicate public slug returned by the creation workflow", async () => {
        get.mockResolvedValue(
            statusResponse("Rejected", {
                rejectionReason: "An event with this public slug already exists",
            }),
        );
        renderForm();
        fillValidForm();

        await submitAndAdvanceFirstPoll();

        expect(post).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/events`, {
            name: "Admitto Summit",
            publicSlug: "admitto-summit",
            websiteUrl: "https://example.com/event",
            baseUrl: "https://register.example.com",
            timeZone: "UTC",
            startsAt: "2026-06-01T09:00:00.000Z",
            endsAt: "2026-06-02T18:00:00.000Z",
        });
        expect(screen.getByRole("alert")).toHaveTextContent(
            "Creation rejected: An event with this public slug already exists",
        );
        expect(routerMock.push).not.toHaveBeenCalled();
        await expectFormReenabled();
    });

    it("displays a timeout when polling returns Expired", async () => {
        get.mockResolvedValue(statusResponse("Expired"));
        renderForm();
        fillValidForm();

        await submitAndAdvanceFirstPoll();

        expect(screen.getByText("Creation timed out, please try again.")).toBeInTheDocument();
        await expectFormReenabled();
    });

    it("tolerates an initial 404 and continues polling", async () => {
        get.mockRejectedValueOnce(
            new FormError({
                status: 404,
                title: "Not found",
                detail: "Creation request is not visible yet.",
                errors: {},
            }),
        );
        get.mockResolvedValueOnce(statusResponse("Created", { ticketedEventId: EVENT_ID }));
        renderForm();
        fillValidForm();

        await submitAndAdvanceFirstPoll();
        expect(get).toHaveBeenCalledTimes(1);

        await act(async () => {
            await vi.advanceTimersByTimeAsync(700);
        });

        expect(get).toHaveBeenCalledTimes(2);
        expect(routerMock.push).toHaveBeenCalledWith(`/teams/${TEAM_ID}/events/${EVENT_ID}`);
    });
});
