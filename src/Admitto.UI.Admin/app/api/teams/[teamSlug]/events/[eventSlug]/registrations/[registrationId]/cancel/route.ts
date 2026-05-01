import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelRegistration } from "@/lib/admitto-api/generated/sdk.gen";
import type { CancelRegistrationHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; registrationId: string }> },
) {
    const { teamSlug, eventSlug, registrationId } = await params;
    const body = await request.json() as CancelRegistrationHttpRequest;
    return callAdmittoApi(() => cancelRegistration({ path: { teamSlug, eventSlug, registrationId }, body }));
}
