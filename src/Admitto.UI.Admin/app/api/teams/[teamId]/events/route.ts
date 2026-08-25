import {NextResponse} from "next/server";
import {callAdmittoApi} from "@/lib/admitto-api/admitto-client";
import {requestTicketedEventCreation, getTicketedEvents} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamId: string }> }) {

    const {teamId} = await params;

    return callAdmittoApi(() => getTicketedEvents({path: {teamId}}));
}

export async function POST(
    request: Request,
    {params}: { params: Promise<{ teamId: string }> }) {

    const {teamId} = await params;
    const body = await request.json();

    return callAdmittoApi(
        () => requestTicketedEventCreation({path: {teamId}, body}),
        {
            onSuccess: (result) => {
                const location = result.response.headers.get("Location") ?? "";
                const creationRequestId = location.split("/").filter(Boolean).pop() ?? null;
                return NextResponse.json(
                    {creationRequestId, statusUrl: location},
                    {status: result.response.status, headers: location ? {Location: location} : undefined}
                );
            },
        },
    );
}
