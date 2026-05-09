import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { listTeamMembers, assignTeamMembership } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    return callAdmittoApi(() => listTeamMembers({ path: { teamId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => assignTeamMembership({ path: { teamId }, body }));
}
