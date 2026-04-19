import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { closeRegistration } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => closeRegistration({ path: { teamSlug, eventSlug } }));
}
