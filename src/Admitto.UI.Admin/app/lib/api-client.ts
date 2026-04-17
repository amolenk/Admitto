import { FormError } from "@/components/form-error";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(path, init);

    if (!res.ok) {
        const contentType = res.headers.get("content-type") ?? "";

        if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
            const body = await res.json().catch(() => null);

            if (body && body.status) {
                throw new FormError(body);
            }
        }

        const msg = await res.text().catch(() => "");
        throw new FormError({
            status: res.status,
            title: "Request Failed",
            detail: msg || `Request failed with status ${res.status}`,
            errors: {},
        });
    }

    return (await res.json()) as T;
}

export const apiClient = {
    get: <T>(path: string) => request<T>(path),

    post: <T>(path: string, body?: unknown) =>
        request<T>(path, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            ...(body !== undefined && { body: JSON.stringify(body) }),
        }),

    put: <T>(path: string, body?: unknown) =>
        request<T>(path, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            ...(body !== undefined && { body: JSON.stringify(body) }),
        }),
};
