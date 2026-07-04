import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { requestTicketConfirmationResend } from "@/lib/admitto-api/generated/sdk.gen";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; registrationId: string }> },
) {
    const { teamId, eventId, registrationId } = await params;
    return callAdmittoApi(() => requestTicketConfirmationResend({ path: { teamId, eventId, registrationId } }));
}
