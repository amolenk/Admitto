import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getApiKeys, createApiKey } from "@/lib/admitto-api/generated/sdk.gen";
import type { CreateApiKeyHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    return callAdmittoApi(() => getApiKeys({ path: { teamId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    const body = await request.json() as CreateApiKeyHttpRequest;
    return callAdmittoApi(() => createApiKey({ path: { teamId }, body }));
}
