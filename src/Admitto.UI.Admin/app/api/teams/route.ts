import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { createTeam, getTeams } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET() {
    return callAdmittoApi(() => getTeams());
}

export async function POST(req: Request) {
    const body = await req.json();
    return callAdmittoApi(() => createTeam({ body }));
}
