import { lookupCheckInCandidates } from "@/lib/admitto-api/generated/sdk.gen";
import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
export async function GET(request: Request, { params }: { params: Promise<{ teamId: string; eventId: string }> }) {
    const { teamId, eventId } = await params;
    const query = new URL(request.url).searchParams.get("query") ?? undefined;
    return callAdmittoApi(() => lookupCheckInCandidates({ path: { teamId, eventId }, query: { query } }));
}
