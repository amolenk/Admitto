import {callAdmittoApi} from "@/lib/admitto-api/admitto-client";
import {getTicketedEvents} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamSlug: string }> }) {

    const {teamSlug} = await params;

    return callAdmittoApi(() => getTicketedEvents({path: {teamSlug}}));
}