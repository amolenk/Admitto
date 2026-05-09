import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getAttendeeEmails } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; registrationId: string }> },
) {
    const { teamId, eventId, registrationId } = await params;
    return callAdmittoApi(() => getAttendeeEmails({ path: { teamId, eventId, registrationId } }));
}
