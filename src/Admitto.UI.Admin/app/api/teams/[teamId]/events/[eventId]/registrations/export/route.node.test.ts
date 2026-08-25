import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/auth", () => ({
    auth: { api: { getAccessToken: vi.fn(async () => ({ accessToken: "test-token" })) } },
}));
vi.mock("next/headers", () => ({ headers: vi.fn(async () => new Headers()) }));
vi.mock("@/lib/admitto-api/generated", () => ({ exportRegistrationsCsv: vi.fn() }));
vi.mock("@/lib/admitto-api/admitto-client", async () => {
    const actual = await vi.importActual<typeof import("@/lib/admitto-api/admitto-client")>(
        "@/lib/admitto-api/admitto-client",
    );
    return { ...actual, callAdmittoApi: vi.fn(actual.callAdmittoApi) };
});

const { exportRegistrationsCsv } = await import("@/lib/admitto-api/generated");
const { callAdmittoApi } = await import("@/lib/admitto-api/admitto-client");
const { GET } = await import("./route");

const exportCsv = vi.mocked(exportRegistrationsCsv);
const callApi = vi.mocked(callAdmittoApi);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";

const CSV = "email,name\na@b.com,Ada\n";

function call() {
    return GET(new Request("http://localhost/export"), {
        params: Promise.resolve({ teamId: TEAM_ID, eventId: EVENT_ID }),
    });
}

/** Shapes a hey-api success result carrying the CSV text and the given headers. */
function sdkSuccess(headers: Record<string, string> = {}) {
    return { data: CSV, response: new Response(null, { status: 200, headers }) };
}

describe("registrations CSV export route", () => {
    beforeEach(() => {
        vi.spyOn(console, "error").mockImplementation(() => {});
    });

    // Given the backend returns the CSV together with its own filename
    // When the route forwards it
    // Then the body, the CSV content type, and the upstream filename all reach the browser,
    // so the download is named as the backend intended
    it("forwards the CSV and the upstream filename", async () => {
        exportCsv.mockResolvedValue(
            sdkSuccess({ "content-disposition": 'attachment; filename="registrations-2026.csv"' }) as never,
        );

        const response = await call();

        expect(response.status).toBe(200);
        await expect(response.text()).resolves.toBe(CSV);
        expect(response.headers.get("content-type")).toBe("text/csv; charset=utf-8");
        expect(response.headers.get("content-disposition")).toBe(
            'attachment; filename="registrations-2026.csv"',
        );
        expect(exportCsv).toHaveBeenCalledWith({ path: { teamId: TEAM_ID, eventId: EVENT_ID } });
        expect(callApi).toHaveBeenCalledWith(expect.any(Function), {
            onSuccess: expect.any(Function),
        });
    });

    // Given the backend omits content-disposition
    // When the route forwards the CSV
    // Then a filename is generated, so the browser does not save the file as the route name
    it("generates a filename when the backend omits content-disposition", async () => {
        exportCsv.mockResolvedValue(sdkSuccess() as never);

        const response = await call();

        expect(response.headers.get("content-disposition")).toBe(
            `attachment; filename="registrations-${EVENT_ID}.csv"`,
        );
    });

    // Given the caller is not permitted to export
    // When the backend rejects the request
    // Then the error body and status are forwarded as JSON rather than downloaded as a CSV
    it("forwards an upstream error body and status", async () => {
        const problem = { status: 403, title: "Forbidden" };
        exportCsv.mockResolvedValue({ error: problem, response: new Response(null, { status: 403 }) } as never);

        const response = await call();

        expect(response.status).toBe(403);
        await expect(response.json()).resolves.toEqual(problem);
    });

    // The shared wrapper supplies the standard fallback for an empty upstream error.
    it("substitutes a generic error when the upstream body is empty", async () => {
        exportCsv.mockResolvedValue({ response: new Response(null, { status: 502 }) } as never);

        const response = await call();

        expect(response.status).toBe(502);
        await expect(response.json()).resolves.toEqual({ error: "Upstream API error" });
    });

    // The shared wrapper rejects unexpected SDK result shapes.
    it("returns 500 for an unrecognised result shape", async () => {
        exportCsv.mockResolvedValue({} as never);

        const response = await call();

        expect(response.status).toBe(500);
        await expect(response.json()).resolves.toEqual({ error: "Unexpected API response shape" });
    });

    // Given the session has expired, so the SDK throws a 401
    // When the route handles it
    // Then a bare 401 is returned for the browser to redirect on
    it("maps a thrown 401 to a bare 401", async () => {
        exportCsv.mockRejectedValue({ response: { status: 401 } });

        const response = await call();

        expect(response.status).toBe(401);
        await expect(response.text()).resolves.toBe("");
    });

    // Given an unexpected failure
    // When the route handles it
    // Then a 500 is returned and the cause is logged
    it("maps an unexpected throw to 500 and logs it", async () => {
        exportCsv.mockRejectedValue(new Error("socket hang up"));

        const response = await call();

        expect(response.status).toBe(500);
        await expect(response.json()).resolves.toEqual({ error: "Internal server error" });
        expect(console.error).toHaveBeenCalled();
    });
});
