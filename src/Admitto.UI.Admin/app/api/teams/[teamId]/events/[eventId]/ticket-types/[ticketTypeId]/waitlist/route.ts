import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getWaitlistDetailsHttpEndpoint } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; ticketTypeId: string }> }
) {
    const { teamId, eventId, ticketTypeId } = await params;
    return callAdmittoApi(() =>
        getWaitlistDetailsHttpEndpoint({ path: { teamId, eventId, ticketTypeId } })
    );
}
