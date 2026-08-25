import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/auth", () => ({
    auth: { api: { getAccessToken: vi.fn(async () => ({ accessToken: "test-token" })) } },
}));
vi.mock("next/headers", () => ({ headers: vi.fn(async () => new Headers()) }));
vi.mock("@/lib/admitto-api/generated", () => ({
    getTicketedEvents: vi.fn(),
    requestTicketedEventCreation: vi.fn(),
}));
vi.mock("@/lib/admitto-api/admitto-client", async () => {
    const actual = await vi.importActual<typeof import("@/lib/admitto-api/admitto-client")>(
        "@/lib/admitto-api/admitto-client",
    );
    return { ...actual, callAdmittoApi: vi.fn(actual.callAdmittoApi) };
});

const { requestTicketedEventCreation } = await import("@/lib/admitto-api/generated");
const { callAdmittoApi } = await import("@/lib/admitto-api/admitto-client");
const { POST } = await import("./route");

const requestCreation = vi.mocked(requestTicketedEventCreation);
const callApi = vi.mocked(callAdmittoApi);
const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const CREATION_REQUEST_ID = "22222222-2222-2222-2222-222222222222";

function call(body: unknown) {
    return POST(
        new Request("http://localhost/events", {
            method: "POST",
            body: JSON.stringify(body),
            headers: { "content-type": "application/json" },
        }),
        { params: Promise.resolve({ teamId: TEAM_ID }) },
    );
}

describe("event creation route", () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it("preserves the creation payload, status and Location header", async () => {
        const body = { name: "DevConf 2026" };
        const location = `/admin/teams/${TEAM_ID}/event-creations/${CREATION_REQUEST_ID}`;
        requestCreation.mockResolvedValue({
            response: new Response(null, { status: 202, headers: { Location: location } }),
        } as never);

        const response = await call(body);

        expect(response.status).toBe(202);
        await expect(response.json()).resolves.toEqual({
            creationRequestId: CREATION_REQUEST_ID,
            statusUrl: location,
        });
        expect(response.headers.get("Location")).toBe(location);
        expect(requestCreation).toHaveBeenCalledWith({ path: { teamId: TEAM_ID }, body });
        expect(callApi).toHaveBeenCalledWith(expect.any(Function), {
            onSuccess: expect.any(Function),
        });
    });
});
