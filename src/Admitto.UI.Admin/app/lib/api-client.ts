export class ApiError extends Error {
    constructor(
        public readonly status: number,
        message: string,
    ) {
        super(message);
        this.name = "ApiError";
    }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(path, init);

    if (!res.ok) {
        const msg = await res.text().catch(() => "");
        throw new ApiError(res.status, msg || `Request failed with status ${res.status}`);
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
};
