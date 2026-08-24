import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import * as z from "zod";

import { FormError } from "@/components/form-error";
import { validationProblemDetails } from "@/test-utils/builders";

import { useCustomForm } from "./use-custom-form";

// `useCustomForm` backs all 14 forms in the app. It wraps react-hook-form with one piece of
// behaviour of its own: translating a thrown FormError into per-field errors plus a general
// error banner. Everything below targets that translation, not react-hook-form itself.

const schema = z.object({
    name: z.string().min(1, "Name is required"),
    slug: z.string().min(1, "Slug is required"),
});

type Values = z.infer<typeof schema>;

function renderCustomForm() {
    return renderHook(() => {
        const form = useCustomForm<Values>(schema, { name: "Team", slug: "team" });

        // `formState` is a Proxy that only subscribes to the properties read during render.
        // A component reads `errors` via <FormMessage>; a bare hook does not, so without this
        // access react-hook-form never re-renders on an error change and `formState.errors`
        // stays permanently empty.
        void form.formState.errors;

        return form;
    });
}

/** Submits with a handler that throws the given error, as a failing API call would. */
async function submitFailing(
    form: ReturnType<typeof renderCustomForm>["result"],
    error: unknown,
) {
    await act(async () => {
        await form.current.submit(async () => {
            throw error;
        })();
    });
}

describe("useCustomForm server errors", () => {
    // Given the backend rejects the submission with errors on several fields
    // When the submit handler throws the FormError
    // Then each field carries its own message, tagged as a server error so that
    // react-hook-form clears it on the next successful validation
    it("attaches a FormError's field errors to the matching fields", async () => {
        const { result } = renderCustomForm();

        await submitFailing(
            result,
            new FormError(
                validationProblemDetails({
                    name: ["A team with this name already exists"],
                    slug: ["Slug is taken"],
                }),
            ),
        );

        expect(result.current.formState.errors.name).toMatchObject({
            type: "server",
            message: "A team with this name already exists",
        });
        expect(result.current.formState.errors.slug?.message).toBe("Slug is taken");
    });

    // Given a field with several validation messages
    // When the error is applied
    // Then only the first is shown, since a field renders a single message
    it("shows the first message when a field has several", async () => {
        const { result } = renderCustomForm();

        await submitFailing(
            result,
            new FormError(validationProblemDetails({ name: ["Too short", "Not unique"] })),
        );

        expect(result.current.formState.errors.name?.message).toBe("Too short");
    });

    // Given a payload where a field's messages arrive as a bare string rather than an array
    // When the error is applied
    // Then the string is used as-is instead of being indexed into characters
    it("tolerates a single string in place of a message array", async () => {
        const { result } = renderCustomForm();

        await submitFailing(
            result,
            new FormError({
                status: 400,
                title: "Validation failed",
                errors: { name: "Name is taken" as unknown as string[] },
            }),
        );

        expect(result.current.formState.errors.name?.message).toBe("Name is taken");
    });

    // Given a FormError with no field errors, such as a concurrency conflict
    // When it is handled
    // Then only the general banner is populated, and no field is marked invalid
    it("populates only the general error when there are no field errors", async () => {
        const { result } = renderCustomForm();

        await submitFailing(
            result,
            new FormError({
                status: 409,
                title: "Conflict",
                detail: "The team was modified by someone else.",
            }),
        );

        expect(result.current.generalError).toEqual({
            title: "Conflict",
            detail: "The team was modified by someone else.",
        });
        expect(result.current.formState.errors).toEqual({});
    });

    // Given something other than a FormError — a network failure or a programming error
    // When it escapes the submit handler
    // Then it is reported generically rather than leaking the raw message to the user
    it("reports a non-FormError generically", async () => {
        const { result } = renderCustomForm();

        await submitFailing(result, new Error("socket hang up"));

        expect(result.current.generalError).toEqual({
            title: "Unexpected Error",
            detail: "An unexpected error occurred.",
        });
    });

    // Given a submission that already failed, leaving a banner on screen
    // When the form is submitted again and succeeds
    // Then the stale banner is cleared rather than persisting over a successful save
    it("clears a stale general error on the next submit", async () => {
        const { result } = renderCustomForm();
        await submitFailing(result, new Error("boom"));
        expect(result.current.generalError).not.toBeNull();

        await act(async () => {
            await result.current.submit(async () => {})();
        });

        expect(result.current.generalError).toBeNull();
    });
});

describe("useCustomForm validation", () => {
    // Given values that fail the zod schema
    // When the form is submitted
    // Then the handler never runs, so no request is made, and the schema message is shown
    it("skips the handler and reports the schema message when values are invalid", async () => {
        const { result } = renderCustomForm();
        const onValid = vi.fn();

        act(() => {
            result.current.setValue("name", "");
        });
        await act(async () => {
            await result.current.submit(onValid)();
        });

        expect(onValid).not.toHaveBeenCalled();
        expect(result.current.formState.errors.name?.message).toBe("Name is required");
    });

    // Given valid values
    // When the form is submitted
    // Then the handler receives them and no error is recorded
    it("passes valid values to the handler", async () => {
        const { result } = renderCustomForm();
        const onValid = vi.fn();

        await act(async () => {
            await result.current.submit(onValid)();
        });

        expect(onValid).toHaveBeenCalledWith({ name: "Team", slug: "team" });
        expect(result.current.generalError).toBeNull();
    });
});
