import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { addTicketType, getTicketTypes } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getTicketTypes({ path: { teamId, eventId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => addTicketType({ path: { teamId, eventId }, body }));
}
