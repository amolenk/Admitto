import {getTicketedEvents, TicketedEventDto} from "@/lib/admitto-api/generated";
import {NextResponse} from 'next/server'

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamSlug: string }> }) {

    const teamSlug = (await params).teamSlug;

    try {
        const response = await getTicketedEvents({
            path: {teamSlug},
        });

        if (!response.data?.ticketedEvents) {
            return NextResponse.json(
                {error: "Invalid response from Admitto API"},
                {status: 502}
            );
        }

        const data: TicketedEventDto[] = response.data.ticketedEvents;
        return NextResponse.json(data);

    } catch (err) {
        console.error("Failed to fetch ticketed events", err);

        return NextResponse.json(
            {error: "Failed to fetch ticketed events"},
            {status: 502}
        );
    }
}