import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getTeams } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET() {
    return callAdmittoApi(() => getTeams());
}



// export async function POST(req: NextRequest) {
//     try {
//         const {name} = await req.json()

//         const response = await createTeam({body: {name}})

//         if (!response.response.ok) {
//             return NextResponse.json(response.error, { status: response.response.status })
//         }

//         return NextResponse.json(response.data, { status: response.response.status })

//     } catch (error) {
//         console.error(error)
//         return NextResponse.json({ error: "An unexpected error occurred" }, { status: 500 })
//     }
// }
