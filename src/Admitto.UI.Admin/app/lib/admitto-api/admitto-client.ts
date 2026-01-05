import {auth} from "@/lib/auth";
import {cookies, headers} from 'next/headers';
import {NextResponse} from "next/server";
import {type CreateClientConfig} from './generated/client.gen';
import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/config";

export const createClientConfig: CreateClientConfig = (config) => ({
    ...config,
    baseUrl: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100",
    auth: async () => {
        console.log('Fetching access token for Admitto API call');

        const { accessToken } = await auth.api.getAccessToken({
            body: {
                providerId: process.env.AUTH_PROVIDER_ID as string || "keycloak",
            },
            headers: await headers()
        });

        console.log(accessToken);
        console.log('DONE AQUIRING TOKEN');
        return accessToken ?? ""; // HeyAPI will NOT add Authorization if empty
    },
});

export async function callAdmittoApi<T>(
    fn: () => Promise<T>
): Promise<NextResponse> {

    console.log("Calling admitto api");

    try {
        const result = await callWithRefresh(fn);

        if (result === "UNAUTHORIZED") {
            return new NextResponse(null, { status: 401 });
        }

        const typed = result as any;

        // Success path
        if (typed?.response?.ok) {
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

async function callWithRefresh<T>(fn: () => Promise<T>): Promise<T | "UNAUTHORIZED"> {
    let firstResult: any;

    try {
        firstResult = await fn();
    } catch (err: any) {
        // HeyAPI throws for non-2xx, including 401
        if (err?.response?.status === 401) {
            firstResult = err;
        } else {
            throw err; // Unknown error => let caller handle 500
        }
    }

    // If the first attempt is not unauthorized → return it
    const status =
        firstResult?.response?.status ??
        firstResult?.status ??
        firstResult?.response?.statusCode ??
        null;

    if (status !== 401) {
        return firstResult;
    }

    // Attempt token refresh
    const refreshRes = await fetch(`${process.env.NEXT_PUBLIC_APP_URL}/api/auth/refresh`, {
        method: "GET",
        credentials: "include",
    });

    if (!refreshRes.ok) {
        return "UNAUTHORIZED";
    }

    // Retry once with a fresh token
    try {
        return await fn();
    } catch (err: any) {
        if (err?.response?.status === 401) {
            return "UNAUTHORIZED";
        }
        throw err;
    }
}