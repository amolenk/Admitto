import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getApiKeys, createApiKey } from "@/lib/admitto-api/generated/sdk.gen";
import type { CreateApiKeyHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    return callAdmittoApi(() => getApiKeys({ path: { teamSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json() as CreateApiKeyHttpRequest;
    return callAdmittoApi(() => createApiKey({ path: { teamSlug }, body }));
}
