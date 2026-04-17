import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getTeam, updateTeam } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    return callAdmittoApi(() => getTeam({ path: { teamSlug } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => updateTeam({ path: { teamSlug }, body }));
}
