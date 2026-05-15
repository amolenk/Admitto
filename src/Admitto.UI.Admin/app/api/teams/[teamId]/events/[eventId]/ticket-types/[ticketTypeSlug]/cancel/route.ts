import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelTicketType } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    {
        params,
    }: {
        params: Promise<{ teamId: string; eventId: string; ticketTypeSlug: string }>;
    }
) {
    const { teamId, eventId, ticketTypeSlug: ticketTypeId } = await params;
    return callAdmittoApi(() =>
        cancelTicketType({ path: { teamId, eventId, ticketTypeId } })
    );
}
