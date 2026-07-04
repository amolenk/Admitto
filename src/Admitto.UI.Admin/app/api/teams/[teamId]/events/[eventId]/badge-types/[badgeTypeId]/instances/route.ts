import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { listBadgeInstances, addBadgeInstance } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string }> }
) {
    const { teamId, eventId, badgeTypeId } = await params;
    return callAdmittoApi(() => listBadgeInstances({ path: { teamId, eventId, badgeTypeId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string }> }
) {
    const { teamId, eventId, badgeTypeId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => addBadgeInstance({ path: { teamId, eventId, badgeTypeId }, body }));
}
