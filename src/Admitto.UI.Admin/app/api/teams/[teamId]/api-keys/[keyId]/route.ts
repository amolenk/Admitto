import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { revokeApiKey } from "@/lib/admitto-api/generated/sdk.gen";

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; keyId: string }> }
) {
    const { teamId, keyId } = await params;
    return callAdmittoApi(() => revokeApiKey({ path: { teamId, keyId } }));
}
