import {auth} from "@/lib/auth";
import {headers} from 'next/headers';
import {NextResponse} from "next/server";
import {type CreateClientConfig} from './generated/client.gen';

export const createClientConfig: CreateClientConfig = (config) => ({
    ...config,
    baseUrl: process.env.ADMITTO_API_URL ?? "http://localhost:5100",
    auth: async () => {

        try {
            const { accessToken } = await auth.api.getAccessToken({
                body: {
                    providerId: "generic-oauth",
                },
                headers: await headers()
            });

            return accessToken ?? "";
        } catch {
            // Token expired and refresh failed — return empty so the backend
            // returns 401, which callAdmittoApi forwards to the client.
            return "";
        }
    },
});

export async function callAdmittoApi<T>(
    fn: () => Promise<T>
): Promise<NextResponse> {
    try {
        const result = await fn();

        if (result === "UNAUTHORIZED") {
            return new NextResponse(null, { status: 401 });
        }

        const typed = result as any;

        // Success path
        if (typed?.response?.ok) {
            if (typed.response.status === 204) {
                return new NextResponse(null, { status: 204 });
            }
            return NextResponse.json(typed.data, {
                status: typed.response.status,
            });
        }

        // Error returned in structured form by HeyAPI
        if (typed?.response?.status) {
            return NextResponse.json(
                typed?.error ?? { error: "Upstream API error" },
                { status: typed.response.status }
            );
        }

        // Unexpected shape
        return NextResponse.json(
            { error: "Unexpected API response shape" },
            { status: 500 }
        );
    } catch (err: any) {
        // HeyAPI throws in weird cases or code bug
        console.error("apiProxy unexpected error:", err);

        if (err?.response?.status === 401) {
            return new NextResponse(null, { status: 401 });
        }

        return NextResponse.json({ error: "Internal server error" }, { status: 500 });
    }
}

/**
 * Fallback proxy that calls the backend admin API directly with the same
 * bearer-token auth as `callAdmittoApi`, but without going through the
 * generated SDK. Used for endpoints not yet present in the generated client
 * (pending an ApiClient regeneration).
 */
export async function proxyAdmittoApi(
    method: string,
    path: string,
    body?: unknown,
): Promise<NextResponse> {
    const baseUrl = process.env.ADMITTO_API_URL ?? "http://localhost:5100";

    let accessToken = "";
    try {
        const result = await auth.api.getAccessToken({
            body: { providerId: "generic-oauth" },
            headers: await headers(),
        });
        accessToken = result.accessToken ?? "";
    } catch {
        accessToken = "";
    }

    const upstream = await fetch(`${baseUrl}${path}`, {
        method,
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${accessToken}`,
        },
        body: body === undefined ? undefined : JSON.stringify(body),
    });

    if (upstream.status === 204) {
        return new NextResponse(null, { status: 204 });
    }

    const text = await upstream.text();
    return new NextResponse(text, {
        status: upstream.status,
        headers: { "Content-Type": upstream.headers.get("Content-Type") ?? "application/json" },
    });
}
