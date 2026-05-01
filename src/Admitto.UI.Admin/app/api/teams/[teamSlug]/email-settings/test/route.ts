import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { testTeamEmailSettings } from "@/lib/admitto-api/generated/sdk.gen";
import type { SendTestEmailHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json() as SendTestEmailHttpRequest;

    return callAdmittoApi(() => testTeamEmailSettings({ path: { teamSlug }, body }));
}
