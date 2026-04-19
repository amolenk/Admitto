import {callAdmittoApi} from "@/lib/admitto-api/admitto-client";
import {createTicketedEvent, getTicketedEvents} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamSlug: string }> }) {

    const {teamSlug} = await params;

    return callAdmittoApi(() => getTicketedEvents({path: {teamSlug}}));
}

export async function POST(
    request: Request,
    {params}: { params: Promise<{ teamSlug: string }> }) {

    const {teamSlug} = await params;
    const body = await request.json();

    return callAdmittoApi(() => createTicketedEvent({path: {teamSlug}, body}));
}