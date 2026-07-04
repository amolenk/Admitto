import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getTicketedEventDetails, updateTicketedEventDetails } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getTicketedEventDetails({ path: { teamId, eventId } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => updateTicketedEventDetails({ path: { teamId, eventId }, body }));
}
