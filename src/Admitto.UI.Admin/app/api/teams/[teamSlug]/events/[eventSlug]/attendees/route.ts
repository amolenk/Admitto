import {AttendeeDto, getAttendees} from "@/lib/admitto-api/generated";
import {NextResponse} from 'next/server'

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamSlug: string, eventSlug: string }> }) {

    const {teamSlug, eventSlug} = await params;

    try {
        const response = await getAttendees({
            path: {teamSlug, eventSlug},
        });

        if (!response.data?.attendees) {
            return NextResponse.json(
                {error: "Invalid response from Admitto API"},
                {status: 502}
            );
        }

        const data: AttendeeDto[] = response.data.attendees;
        return NextResponse.json(data);

    } catch (err) {
        console.error("Failed to fetch attendees", err);

        return NextResponse.json(
            {error: "Failed to fetch attendees"},
            {status: 502}
        );
    }
}