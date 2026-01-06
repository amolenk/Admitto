import { NextResponse } from 'next/server'

export async function GET(teamId: string) {

    console.log(teamId);

    const data = [
        {
            id: "event-1",
            name: "Event 1"
        }];

    return NextResponse.json(data, { status: 200 });
}