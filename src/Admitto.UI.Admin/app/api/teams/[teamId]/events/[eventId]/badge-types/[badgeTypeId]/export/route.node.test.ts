import { beforeEach, describe, expect, it, vi } from "vitest";

// A hand-copied twin of the registrations export route (see
// ../../../registrations/export/route.node.test.ts). Both are covered separately on purpose:
// they are duplicated source, so a fix applied to one does not reach the other.

vi.mock("@/lib/admitto-api/generated", () => ({ exportBadgeCsv: vi.fn() }));

const { exportBadgeCsv } = await import("@/lib/admitto-api/generated");
const { GET } = await import("./route");

const exportCsv = vi.mocked(exportBadgeCsv);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";
const BADGE_TYPE_ID = "33333333-3333-3333-3333-333333333333";

const CSV = "badge,name\nSPEAKER,Ada\n";

function call() {
    return GET(new Request("http://localhost/export"), {
        params: Promise.resolve({
            teamId: TEAM_ID,
            eventId: EVENT_ID,
            badgeTypeId: BADGE_TYPE_ID,
        }),
    });
}

/** Shapes a hey-api success result carrying the CSV text and the given headers. */
function sdkSuccess(headers: Record<string, string> = {}) {
    return { data: CSV, response: new Response(null, { status: 200, headers }) };
}

describe("badge CSV export route", () => {
    beforeEach(() => {
        vi.spyOn(console, "error").mockImplementation(() => {});
    });

    // Given the backend returns the CSV together with its own filename
    // When the route forwards it
    // Then the body, the CSV content type, and the upstream filename all reach the browser
    it("forwards the CSV and the upstream filename", async () => {
        exportCsv.mockResolvedValue(
            sdkSuccess({ "content-disposition": 'attachment; filename="speakers.csv"' }) as never,
        );

        const response = await call();

        expect(response.status).toBe(200);
        await expect(response.text()).resolves.toBe(CSV);
        expect(response.headers.get("content-type")).toBe("text/csv; charset=utf-8");
        expect(response.headers.get("content-disposition")).toBe(
            'attachment; filename="speakers.csv"',
        );
        expect(exportCsv).toHaveBeenCalledWith({
            path: { teamId: TEAM_ID, eventId: EVENT_ID, badgeTypeId: BADGE_TYPE_ID },
        });
    });

    // Given the backend omits content-disposition
    // When the route forwards the CSV
    // Then a filename is generated from the badge type, not the event
    it("generates a badge filename when the backend omits content-disposition", async () => {
        exportCsv.mockResolvedValue(sdkSuccess() as never);

        const response = await call();

        expect(response.headers.get("content-disposition")).toBe(
            `attachment; filename="badges-${BADGE_TYPE_ID}.csv"`,
        );
    });

    // Given the badge type does not exist
    // When the backend rejects the request
    // Then the error body and status are forwarded as JSON
    it("forwards an upstream error body and status", async () => {
        const problem = { status: 404, title: "Not Found" };
        exportCsv.mockResolvedValue({ error: problem, response: new Response(null, { status: 404 }) } as never);

        const response = await call();

        expect(response.status).toBe(404);
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
