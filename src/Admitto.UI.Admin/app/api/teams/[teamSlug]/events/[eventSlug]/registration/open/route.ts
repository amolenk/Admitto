import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { openRegistration } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => openRegistration({ path: { teamSlug, eventSlug } }));
}
