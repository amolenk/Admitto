import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { updateTicketType } from "@/lib/admitto-api/generated";

export async function PUT(
    request: Request,
    {
        params,
    }: {
        params: Promise<{ teamId: string; eventId: string; ticketTypeId: string }>;
    }
) {
    const { teamId, eventId, ticketTypeId } = await params;
    const body = await request.json();
    return callAdmittoApi(() =>
        updateTicketType({ path: { teamId, eventId, ticketTypeId }, body })
    );
}
