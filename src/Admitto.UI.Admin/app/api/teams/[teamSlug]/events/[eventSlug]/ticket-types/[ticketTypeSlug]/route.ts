import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { updateTicketType } from "@/lib/admitto-api/generated";

export async function PUT(
    request: Request,
    {
        params,
    }: {
        params: Promise<{ teamSlug: string; eventSlug: string; ticketTypeSlug: string }>;
    }
) {
    const { teamSlug, eventSlug, ticketTypeSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() =>
        updateTicketType({ path: { teamSlug, eventSlug, ticketTypeSlug }, body })
    );
}
