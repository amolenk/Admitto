import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { adminReconfirmRegistration } from "@/lib/admitto-api/generated/sdk.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; registrationId: string }> },
) {
    const { teamId, eventId, registrationId } = await params;
    return callAdmittoApi(() => adminReconfirmRegistration({ path: { teamId, eventId, registrationId } }));
}
