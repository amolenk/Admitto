import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { changeTeamMembershipRole, removeTeamMembership } from "@/lib/admitto-api/generated/sdk.gen";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; email: string }> }
) {
    const { teamSlug, email } = await params;
    const body = await request.json();
    return callAdmittoApi(() => changeTeamMembershipRole({ path: { teamSlug, email: decodeURIComponent(email) }, body }));
}

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; email: string }> }
) {
    const { teamSlug, email } = await params;
    return callAdmittoApi(() => removeTeamMembership({ path: { teamSlug, email: decodeURIComponent(email) } }));
}
