import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { resendTeamMemberInvite } from "@/lib/admitto-api/generated/sdk.gen";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; email: string }> }
) {
    const { teamId, email } = await params;
    return callAdmittoApi(() =>
        resendTeamMemberInvite({ path: { teamId, email: decodeURIComponent(email) } })
    );
}
