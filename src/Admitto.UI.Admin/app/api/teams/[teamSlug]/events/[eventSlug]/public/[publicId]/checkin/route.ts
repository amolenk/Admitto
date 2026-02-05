import {checkIn} from "@/lib/admitto-api/generated";
import {NextResponse} from 'next/server'

export async function POST(
    request: Request,
    {params}: { params: Promise<{ teamSlug: string, eventSlug: string, publicId: string }> }) {

    const {teamSlug, eventSlug, publicId} = await params;
    const url = new URL(request.url);
    const signature = url.searchParams.get('signature');

    if (!signature) {
        return NextResponse.json(
            {error: "Missing signature."},
            {status: 400}
        )
    }

    try {
        const response = await checkIn({
            path: {teamSlug, eventSlug, publicId},
            query: {signature}
        });

        return NextResponse.json(response.data);

    } catch (err) {
        return NextResponse.json(
            {error: "Failed to check-in attendee"},
            {status: 502}
        );
    }
}