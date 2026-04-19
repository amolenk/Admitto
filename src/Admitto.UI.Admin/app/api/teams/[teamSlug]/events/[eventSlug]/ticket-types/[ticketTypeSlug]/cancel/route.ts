import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelTicketType } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    {
        params,
    }: {
        params: Promise<{ teamSlug: string; eventSlug: string; ticketTypeSlug: string }>;
    }
) {
    const { teamSlug, eventSlug, ticketTypeSlug } = await params;
    return callAdmittoApi(() =>
        cancelTicketType({ path: { teamSlug, eventSlug, ticketTypeSlug } })
    );
}
