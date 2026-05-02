import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getRegistrationDetails } from "@/lib/admitto-api/generated/sdk.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; registrationId: string }> },
) {
    const { teamSlug, eventSlug, registrationId } = await params;
    return callAdmittoApi(() => getRegistrationDetails({ path: { teamSlug, eventSlug, registrationId } }));
}
