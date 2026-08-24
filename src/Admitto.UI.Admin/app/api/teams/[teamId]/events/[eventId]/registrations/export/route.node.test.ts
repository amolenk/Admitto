import { beforeEach, describe, expect, it, vi } from "vitest";

// The two CSV export routes are the only proxy routes that do not go through
// `callAdmittoApi` — they hand-roll the response so the browser gets a file download rather
// than JSON. That makes them the only proxy routes needing tests of their own.

vi.mock("@/lib/admitto-api/generated", () => ({ exportRegistrationsCsv: vi.fn() }));

const { exportRegistrationsCsv } = await import("@/lib/admitto-api/generated");
const { GET } = await import("./route");

const exportCsv = vi.mocked(exportRegistrationsCsv);

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

    // Given the backend fails without a body
    // When the route forwards it
    // Then a generic export error stands in, keeping the status
    it("substitutes a generic error when the upstream body is empty", async () => {
        exportCsv.mockResolvedValue({ response: new Response(null, { status: 502 }) } as never);

        const response = await call();

        expect(response.status).toBe(502);
        await expect(response.json()).resolves.toEqual({ error: "Export failed" });
    });

    // Given a result matching none of the expected shapes
    // When the route handles it
    // Then it fails with a 500 rather than serving an empty download
    it("returns 500 for an unrecognised result shape", async () => {
        exportCsv.mockResolvedValue({} as never);

        const response = await call();

        expect(response.status).toBe(500);
        await expect(response.json()).resolves.toEqual({ error: "Export failed" });
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
