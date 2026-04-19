import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { addTicketType, getTicketTypes } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getTicketTypes({ path: { teamSlug, eventSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => addTicketType({ path: { teamSlug, eventSlug }, body }));
}
