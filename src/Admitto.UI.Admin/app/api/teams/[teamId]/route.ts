import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getTeam, updateTeam } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    return callAdmittoApi(() => getTeam({ path: { teamId } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => updateTeam({ path: { teamId }, body }));
}
