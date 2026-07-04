import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { archiveTicketedEvent } from "@/lib/admitto-api/generated";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => archiveTicketedEvent({ path: { teamId, eventId }, body }));
}
