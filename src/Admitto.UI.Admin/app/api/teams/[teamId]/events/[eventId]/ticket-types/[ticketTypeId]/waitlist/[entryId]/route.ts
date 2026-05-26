import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { removeWaitlistEntryHttpEndpoint } from "@/lib/admitto-api/generated";

export async function DELETE(
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
        removeWaitlistEntryHttpEndpoint({ path: { teamId, eventId, ticketTypeId, entryId } })
    );
}
