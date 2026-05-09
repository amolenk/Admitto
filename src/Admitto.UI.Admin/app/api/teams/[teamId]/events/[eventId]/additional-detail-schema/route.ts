import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { updateAdditionalDetailSchema } from "@/lib/admitto-api/generated";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => updateAdditionalDetailSchema({ path: { teamId, eventId }, body }));
}
