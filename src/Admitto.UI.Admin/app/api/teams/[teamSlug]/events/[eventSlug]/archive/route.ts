import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { archiveTicketedEvent } from "@/lib/admitto-api/generated";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => archiveTicketedEvent({ path: { teamSlug, eventSlug }, body }));
}
