import {privilegedCheckIn} from "@/lib/admitto-api/generated";
import {NextResponse} from 'next/server'

export async function POST(
    _request: Request,
    {params}: { params: Promise<{ teamSlug: string, eventSlug: string, attendeeId: string }> }) {

    const {teamSlug, eventSlug, attendeeId} = await params;

    console.log('Check-in request for attendee:', attendeeId);

    try {
        const response = await privilegedCheckIn({
            path: {teamSlug, eventSlug, attendeeId},
        });

        return NextResponse.json(response.data);

    } catch (err) {

        console.log(err);

        return NextResponse.json(
            {error: err || "Failed to check-in attendee"},
            {status: 502}
        );
    }
}