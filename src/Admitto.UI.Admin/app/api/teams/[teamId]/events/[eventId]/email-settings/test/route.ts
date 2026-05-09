import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { testEventEmailSettings } from "@/lib/admitto-api/generated/sdk.gen";
import type { SendTestEmailHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json() as SendTestEmailHttpRequest;

    return callAdmittoApi(() => testEventEmailSettings({ path: { teamId, eventId }, body }));
}
