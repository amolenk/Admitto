import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { changeTeamMembershipRole, removeTeamMembership } from "@/lib/admitto-api/generated/sdk.gen";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; email: string }> }
) {
    const { teamId, email } = await params;
    const body = await request.json();
    return callAdmittoApi(() => changeTeamMembershipRole({ path: { teamId, email: decodeURIComponent(email) }, body }));
}

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; email: string }> }
) {
    const { teamId, email } = await params;
    return callAdmittoApi(() => removeTeamMembership({ path: { teamId, email: decodeURIComponent(email) } }));
}
