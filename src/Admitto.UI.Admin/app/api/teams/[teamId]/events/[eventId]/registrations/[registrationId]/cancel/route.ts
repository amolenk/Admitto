import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelRegistration } from "@/lib/admitto-api/generated/sdk.gen";
import type { CancelRegistrationHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; registrationId: string }> },
) {
    const { teamId, eventId, registrationId } = await params;
    const body = await request.json() as CancelRegistrationHttpRequest;
    return callAdmittoApi(() => cancelRegistration({ path: { teamId, eventId, registrationId }, body }));
}
