import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { archiveTeam } from "@/lib/admitto-api/generated/sdk.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => archiveTeam({ path: { teamSlug }, body }));
}
