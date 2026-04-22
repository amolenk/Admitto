import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getTicketedEventDetails, updateTicketedEventDetails } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getTicketedEventDetails({ path: { teamSlug, eventSlug } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => updateTicketedEventDetails({ path: { teamSlug, eventSlug }, body }));
}
