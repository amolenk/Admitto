import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { deleteBadgeType, renameBadgeType } from "@/lib/admitto-api/generated";

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string }> }
) {
    const { teamId, eventId, badgeTypeId } = await params;
    return callAdmittoApi(() => deleteBadgeType({ path: { teamId, eventId, badgeTypeId } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string }> }
) {
    const { teamId, eventId, badgeTypeId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => renameBadgeType({ path: { teamId, eventId, badgeTypeId }, body }));
}
