import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { promoteWaitlistEntry } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    {
        params,
    }: {
        params: Promise<{
            teamId: string;
            eventId: string;
            ticketTypeId: string;
            entryId: string;
        }>;
    }
) {
    const { teamId, eventId, ticketTypeId, entryId } = await params;
    return callAdmittoApi(() =>
        promoteWaitlistEntry({ path: { teamId, eventId, ticketTypeId, entryId } })
    );
}
