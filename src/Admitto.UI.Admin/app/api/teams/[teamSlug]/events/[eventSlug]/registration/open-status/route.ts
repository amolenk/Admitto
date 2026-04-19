import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getRegistrationOpenStatus } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getRegistrationOpenStatus({ path: { teamSlug, eventSlug } }));
}
