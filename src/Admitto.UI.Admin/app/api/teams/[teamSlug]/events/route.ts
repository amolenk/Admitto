import {NextResponse} from "next/server";
import {callAdmittoApi} from "@/lib/admitto-api/admitto-client";
import {requestTicketedEventCreation, getTicketedEvents} from "@/lib/admitto-api/generated";

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

    try {
        const result = await requestTicketedEventCreation({path: {teamSlug}, body});
        const res = (result as any).response as Response | undefined;

        if (res?.ok) {
            const location = res.headers.get("Location") ?? "";
            const creationRequestId = location.split("/").filter(Boolean).pop() ?? null;
            return NextResponse.json(
                {creationRequestId, statusUrl: location},
                {status: res.status, headers: location ? {Location: location} : undefined}
            );
        }

        if (res?.status) {
            return NextResponse.json(
                (result as any).error ?? {error: "Upstream API error"},
                {status: res.status}
            );
        }

        return NextResponse.json({error: "Unexpected API response shape"}, {status: 500});
    } catch (err: any) {
        if (err?.response?.status === 401) {
            return new NextResponse(null, {status: 401});
        }
        console.error("requestTicketedEventCreation unexpected error:", err);
        return NextResponse.json({error: "Internal server error"}, {status: 500});
    }
}