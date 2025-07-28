import {NextRequest, NextResponse} from 'next/server'
import * as ApiClient from '@/api-client'

export async function GET() {
    try {
        const response = await ApiClient.getTeams()

        if (!response.response.ok) {
            return NextResponse.json(response.error, { status: response.response.status })
        }

        return NextResponse.json(response.data, { status: response.response.status })

    } catch (error) {
        console.log(error)
        return NextResponse.json({ error: 'Failed to fetch teams' }, { status: 500 })
    }
}

export async function POST(req: NextRequest) {
    try {
        const {name} = await req.json()

        const response = await ApiClient.createTeam({body: {name}})

        if (!response.response.ok) {
            return NextResponse.json(response.error, { status: response.response.status })
        }

        return NextResponse.json(response.data, { status: response.response.status })

    } catch (error) {
        console.error(error)
        return NextResponse.json({ error: "An unexpected error occurred" }, { status: 500 })
    }
}
