import { getCheckInSummary } from "@/lib/admitto-api/generated/sdk.gen";
import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
export async function GET(_request: Request, { params }: { params: Promise<{ teamId: string; eventId: string }> }) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getCheckInSummary({ path: { teamId, eventId } }));
}
