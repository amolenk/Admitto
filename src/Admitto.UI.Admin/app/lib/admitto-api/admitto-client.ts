import {auth} from "@/lib/auth";
import {headers} from 'next/headers';
import {NextResponse} from "next/server";
import {type CreateClientConfig} from './generated/client.gen';

type ApiResult = { response?: Response; data?: unknown; error?: unknown };
type SuccessfulApiResult = ApiResult & { response: Response };
type CallAdmittoApiOptions = {
    onSuccess?: (result: SuccessfulApiResult) => NextResponse;
};

async function getAccessToken(): Promise<string> {
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
}

export const createClientConfig: CreateClientConfig = (config) => ({
    ...config,
    baseUrl: process.env.ADMITTO_API_URL ?? "http://localhost:5100",
    auth: getAccessToken,
});

export async function callAdmittoApi<T>(
    fn: () => Promise<T>,
    options?: CallAdmittoApiOptions,
): Promise<NextResponse> {
    try {
        const result = await fn();

        if (result === "UNAUTHORIZED") {
            return new NextResponse(null, { status: 401 });
        }

        const typed = result as ApiResult;

        // Success path
        if (typed?.response?.ok) {
            if (options?.onSuccess) {
                return options.onSuccess(typed as SuccessfulApiResult);
            }
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
    } catch (err: unknown) {
        // HeyAPI throws in weird cases or code bug
        console.error("apiProxy unexpected error:", err);

        const response =
            typeof err === "object" && err !== null && "response" in err
                ? (err as { response?: { status?: number } }).response
                : undefined;
        if (response?.status === 401) {
            return new NextResponse(null, { status: 401 });
        }

        return NextResponse.json({ error: "Internal server error" }, { status: 500 });
    }
}
