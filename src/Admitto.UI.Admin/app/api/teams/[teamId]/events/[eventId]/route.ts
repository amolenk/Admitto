import {NextResponse} from 'next/server'

export async function GET(teamId: string, eventId: string)
{
    console.log(teamId);

    const data =
        {
            id: eventId,
            eventName: "Event 1"
        };

    return NextResponse.json(data, { status: 200 });
}
