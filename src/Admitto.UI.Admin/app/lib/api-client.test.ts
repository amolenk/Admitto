import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "./api-client";

// `apiClient` is the browser-side half of the BFF: every component reaches the `/api/*` proxy
// routes through it. It is stubbed at the `fetch` boundary rather than with MSW — one seam,
// no extra machinery — and real `Response` objects are used so body-consumption semantics
// (see "already read" below) behave as they do in the browser.

const fetchMock = vi.fn<typeof fetch>();

/** A response carrying a JSON body, with the content-type the code branches on. */
function jsonResponse(status: number, body: unknown, contentType = "application/json") {
    return new Response(JSON.stringify(body), { status, headers: { "content-type": contentType } });
}

/**
 * Replaces `window.location` so the sign-in redirect is observable. Assigning `href` on
 * jsdom's real location logs a "Not implemented: navigation" error and leaves `href`
 * unchanged, which would make the redirect untestable.
 */
function stubLocation(): { href: string } {
    const location = { href: "http://localhost:3000/teams" };
    Object.defineProperty(window, "location", {
        configurable: true,
        writable: true,
        value: location,
    });
    return location;
}

describe("apiClient error handling", () => {
    beforeEach(() => {
        vi.stubGlobal("fetch", fetchMock);
    });

    // Given the session has expired, so the proxy route answers 401
    // When any request is made
    // Then the browser is sent to sign-in and the promise never settles, so no caller
    // renders or caches data from the failed request
    it("redirects to sign-in and never settles on 401", async () => {
        const location = stubLocation();
        fetchMock.mockResolvedValue(new Response(null, { status: 401 }));
        const settled = vi.fn();

        void apiClient.get("/api/teams").then(settled, settled);

        await vi.waitFor(() => expect(location.href).toBe("/signin"));
        expect(settled).not.toHaveBeenCalled();
    });

    // Given the backend rejects a command with a validation ProblemDetails
    // When the request fails
    // Then a FormError carrying the field errors is thrown, which is what `useCustomForm`
    // needs in order to attach messages to individual fields
    it("throws a FormError built from a problem+json body", async () => {
        fetchMock.mockResolvedValue(
            jsonResponse(
                400,
                {
                    status: 400,
                    title: "Validation failed",
                    detail: "One field is invalid.",
                    errors: { name: ["Name is required"] },
                },
                "application/problem+json",
            ),
        );

        await expect(apiClient.post("/api/teams", {})).rejects.toMatchObject({
            name: "FormError",
            status: 400,
            title: "Validation failed",
            detail: "One field is invalid.",
            errors: { name: ["Name is required"] },
        });
    });

    // Given a JSON error body that is not ProblemDetails (no `status` field)
    // When the request fails
    // Then a FormError is synthesised from the HTTP status instead.
    // The detail falls back to the generated sentence rather than the body text: the body
    // was already consumed by the `json()` attempt, so the follow-up `text()` yields "".
    it("synthesises a FormError when a JSON body is not ProblemDetails", async () => {
        fetchMock.mockResolvedValue(jsonResponse(409, { message: "Already exists" }));

        await expect(apiClient.put("/api/teams/1", {})).rejects.toMatchObject({
            name: "FormError",
            status: 409,
            title: "Request Failed",
            detail: "Request failed with status 409",
            errors: {},
        });
    });

    // Given a plain-text error body, as an unhandled server fault produces
    // When the request fails
    // Then the body text becomes the detail, so the message is not lost
    it("uses a non-JSON error body as the detail", async () => {
        fetchMock.mockResolvedValue(
            new Response("Upstream exploded", {
                status: 500,
                headers: { "content-type": "text/plain" },
            }),
        );

        await expect(apiClient.get("/api/teams")).rejects.toMatchObject({
            status: 500,
            detail: "Upstream exploded",
        });
    });

    // Given a failure with no body at all
    // When the request fails
    // Then the detail falls back to a sentence naming the status
    it("falls back to a status sentence when the error body is empty", async () => {
        fetchMock.mockResolvedValue(new Response(null, { status: 502 }));

        await expect(apiClient.get("/api/teams")).rejects.toMatchObject({
            status: 502,
            title: "Request Failed",
            detail: "Request failed with status 502",
        });
    });

    // Given a response claiming to be JSON but carrying a malformed body
    // When the request fails
    // Then parsing does not blow up; a synthesised FormError is thrown instead
    it("survives a malformed JSON error body", async () => {
        fetchMock.mockResolvedValue(
            new Response("{ not json", { status: 500, headers: { "content-type": "application/json" } }),
        );

        await expect(apiClient.get("/api/teams")).rejects.toMatchObject({
            status: 500,
            title: "Request Failed",
        });
    });
});

describe("apiClient requests", () => {
    beforeEach(() => {
        vi.stubGlobal("fetch", fetchMock);
    });

    /** The `RequestInit` handed to fetch by the call under test. */
    function requestInit() {
        return fetchMock.mock.calls[0]![1];
    }

    // Given a successful request returning a payload
    // When it completes
    // Then the parsed body is returned
    it("returns the parsed body on success", async () => {
        fetchMock.mockResolvedValue(jsonResponse(200, [{ teamId: "1" }]));

        await expect(apiClient.get("/api/teams")).resolves.toEqual([{ teamId: "1" }]);
    });

    // Given a command that succeeds with 204 No Content
    // When it completes
    // Then undefined is returned rather than a JSON-parse failure on an empty body
    it("returns undefined for 204 instead of parsing an empty body", async () => {
        fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

        await expect(apiClient.post("/api/teams", { name: "Team" })).resolves.toBeUndefined();
    });

    // Given a GET
    // When it is issued
    // Then no method or headers are set, so fetch's defaults apply
    it("issues a bare GET", async () => {
        fetchMock.mockResolvedValue(jsonResponse(200, {}));

        await apiClient.get("/api/teams");

        expect(fetchMock).toHaveBeenCalledWith("/api/teams", undefined);
    });

    // Given a POST or PUT with a body
    // When it is issued
    // Then the body is JSON-serialised and the JSON content-type is declared
    it.each([
        ["post", (path: string, body: unknown) => apiClient.post(path, body), "POST"],
        ["put", (path: string, body: unknown) => apiClient.put(path, body), "PUT"],
    ])("serialises the body for %s", async (_label, call, method) => {
        fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

        await call("/api/teams", { name: "Team" });

        expect(fetchMock).toHaveBeenCalledWith("/api/teams", {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ name: "Team" }),
        });
    });

    // Given a POST used purely as a trigger, with no payload
    // When it is issued
    // Then no body is sent at all, rather than the string "undefined"
    it("omits the body entirely when none is given", async () => {
        fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

        await apiClient.post("/api/teams/1/events/2/publish");

        expect(requestInit()).not.toHaveProperty("body");
    });

    // Given a DELETE
    // When it is issued
    // Then only the method is set — no body, no content-type
    it("issues a DELETE with no body", async () => {
        fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

        await apiClient.delete("/api/teams/1");

        expect(requestInit()).toStrictEqual({ method: "DELETE" });
    });
});
