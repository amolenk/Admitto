import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { deleteBadgeInstance, updateBadgeInstance } from "@/lib/admitto-api/generated";

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string; badgeInstanceId: string }> }
) {
    const { teamId, eventId, badgeTypeId, badgeInstanceId } = await params;
    return callAdmittoApi(() =>
        deleteBadgeInstance({ path: { teamId, eventId, badgeTypeId, badgeInstanceId } })
    );
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string; badgeInstanceId: string }> }
) {
    const { teamId, eventId, badgeTypeId, badgeInstanceId } = await params;
    const body = await request.json();
    return callAdmittoApi(() =>
        updateBadgeInstance({ path: { teamId, eventId, badgeTypeId, badgeInstanceId }, body })
    );
}
