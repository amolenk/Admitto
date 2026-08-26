import { beforeEach, describe, expect, it, vi } from "vitest";

// `admitto-client.ts` imports `@/lib/auth`, which constructs a real pg `Pool` at module
// scope, and `next/headers`, which only works inside a request scope. Both are replaced
// here; neither is exercised by `callAdmittoApi` itself.
vi.mock("@/lib/auth", () => ({
    auth: { api: { getAccessToken: vi.fn(async () => ({ accessToken: "test-token" })) } },
}));
vi.mock("next/headers", () => ({ headers: vi.fn(async () => new Headers()) }));

import { callAdmittoApi } from "./admitto-client";

/** Shapes a hey-api success result: the SDK returns `{ data, response }`. */
function sdkSuccess(status: number, data: unknown) {
    return { data, response: new Response(null, { status }) };
}

/** Shapes a hey-api failure result: `response.ok` is false and `error` carries the body. */
function sdkFailure(status: number, error?: unknown) {
    return { error, response: { ok: false, status } };
}

describe("callAdmittoApi", () => {
    beforeEach(() => {
        // The catch branch logs; keep the suite output clean.
        vi.spyOn(console, "error").mockImplementation(() => {});
    });

    // Given the upstream API returns 200 with a payload
    // When the proxy forwards it
    // Then the payload and status are passed through unchanged
    it("forwards a successful payload and status", async () => {
        const response = await callAdmittoApi(async () => sdkSuccess(200, { name: "Early Bird" }));

        expect(response.status).toBe(200);
        await expect(response.json()).resolves.toEqual({ name: "Early Bird" });
    });

    // Given the upstream API returns a non-200 success status
    // When the proxy forwards it
    // Then that exact status is preserved rather than normalised to 200
    it("preserves a 201 status", async () => {
        const response = await callAdmittoApi(async () => sdkSuccess(201, { id: "abc" }));

        expect(response.status).toBe(201);
    });

    // Given the upstream API returns 204 No Content
    // When the proxy forwards it
    // Then an empty 204 is returned, with no attempt to serialise a body
    it("returns an empty body for 204", async () => {
        const response = await callAdmittoApi(async () => sdkSuccess(204, undefined));

        expect(response.status).toBe(204);
        await expect(response.text()).resolves.toBe("");
    });

    // Given the upstream API returns a validation failure with a ProblemDetails body
    // When the proxy forwards it
    // Then the body and status reach the browser intact, so forms can map field errors
    it("forwards an upstream error body and status", async () => {
        const problem = { status: 400, title: "Validation failed", errors: { name: ["Required"] } };

        const response = await callAdmittoApi(async () => sdkFailure(400, problem));

        expect(response.status).toBe(400);
        await expect(response.json()).resolves.toEqual(problem);
    });

    // Given the upstream API fails without a response body
    // When the proxy forwards it
    // Then a generic upstream-error body is substituted, keeping the status
    it("substitutes a generic body when the upstream error is empty", async () => {
        const response = await callAdmittoApi(async () => sdkFailure(502, undefined));

        expect(response.status).toBe(502);
        await expect(response.json()).resolves.toEqual({ error: "Upstream API error" });
    });

    // Given the access token could not be obtained, so the SDK signals UNAUTHORIZED
    // When the proxy handles it
    // Then a bare 401 is returned for the browser to redirect on
    it("returns 401 for an UNAUTHORIZED result", async () => {
        const response = await callAdmittoApi(async () => "UNAUTHORIZED");

        expect(response.status).toBe(401);
    });

    // Given a result that matches none of the expected hey-api shapes
    // When the proxy handles it
    // Then it fails loudly with a 500 rather than returning a misleading success
    it("returns 500 for an unrecognised result shape", async () => {
        const response = await callAdmittoApi(async () => ({ unexpected: true }));

        expect(response.status).toBe(500);
        await expect(response.json()).resolves.toEqual({ error: "Unexpected API response shape" });
    });

    // Given the SDK throws an error carrying a 401 response
    // When the proxy handles it
    // Then it is normalised to a bare 401, not a 500
    it("maps a thrown 401 to a bare 401", async () => {
        const response = await callAdmittoApi(async () => {
            throw { response: { status: 401 } };
        });

        expect(response.status).toBe(401);
    });

    // Given the SDK throws an unexpected error
    // When the proxy handles it
    // Then a 500 is returned and the cause is logged for diagnosis
    it("maps an unexpected throw to 500 and logs it", async () => {
        const response = await callAdmittoApi(async () => {
            throw new Error("socket hang up");
        });

        expect(response.status).toBe(500);
        await expect(response.json()).resolves.toEqual({ error: "Internal server error" });
        expect(console.error).toHaveBeenCalled();
    });
});
