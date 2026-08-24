import { describe, expect, it } from "vitest";

import { validationProblemDetails } from "@/test-utils/builders";

import { FormError } from "./form-error";

// FormError is the single carrier between `api-client` and `useCustomForm`: every field-level
// server error in the app arrives as one of these. Its whole job is normalising a
// ProblemDetails payload whose every property is optional into non-optional fields, so the
// fallbacks below are what forms actually render when the backend is terse.

describe("FormError", () => {
    // Given a fully populated validation ProblemDetails
    // When it is wrapped in a FormError
    // Then every field is carried across unchanged
    it("carries status, title, detail and errors across", () => {
        const error = new FormError(
            validationProblemDetails(
                { name: ["Name is required"] },
                { status: 400, title: "Validation failed", detail: "One field is invalid." },
            ),
        );

        expect(error.status).toBe(400);
        expect(error.title).toBe("Validation failed");
        expect(error.detail).toBe("One field is invalid.");
        expect(error.errors).toEqual({ name: ["Name is required"] });
    });

    // Given an empty ProblemDetails, as a bare non-JSON failure produces
    // When it is wrapped
    // Then generic values stand in, so no form renders "undefined"
    it("falls back to generic values when the payload is empty", () => {
        const error = new FormError({});

        expect(error.status).toBe(500);
        expect(error.title).toBe("Server Error");
        expect(error.detail).toBe("An unexpected error occurred.");
        expect(error.errors).toEqual({});
    });

    // Given a ProblemDetails whose fields are explicitly null rather than absent
    // When it is wrapped
    // Then the same fallbacks apply — null is treated as missing, not as a value
    it("treats explicit nulls as missing", () => {
        const error = new FormError({ status: null, title: null, detail: null });

        expect(error.status).toBe(500);
        expect(error.title).toBe("Server Error");
        expect(error.detail).toBe("An unexpected error occurred.");
    });

    // Given a FormError
    // When it is thrown and caught
    // Then it behaves as a real Error: instanceof holds, and the title is the message
    it("is a real Error carrying the title as its message", () => {
        const error = new FormError(validationProblemDetails({}, { title: "Conflict" }));

        expect(error).toBeInstanceOf(Error);
        expect(error.name).toBe("FormError");
        expect(error.message).toBe("Conflict");
    });

    // Given a payload with no title
    // When it is thrown
    // Then the message still reads sensibly rather than being empty
    it("uses the fallback title as the message when none is given", () => {
        expect(new FormError({}).message).toBe("Server Error");
    });
});
