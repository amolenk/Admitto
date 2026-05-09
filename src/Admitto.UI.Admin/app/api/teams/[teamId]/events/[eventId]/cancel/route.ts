import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelTicketedEvent } from "@/lib/admitto-api/generated";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => cancelTicketedEvent({ path: { teamId, eventId }, body }));
}
