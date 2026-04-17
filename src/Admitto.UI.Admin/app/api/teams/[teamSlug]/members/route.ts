import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { listTeamMembers, assignTeamMembership } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    return callAdmittoApi(() => listTeamMembers({ path: { teamSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => assignTeamMembership({ path: { teamSlug }, body }));
}
