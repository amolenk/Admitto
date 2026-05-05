import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { createBulkEmail, getBulkEmails } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getBulkEmails({ path: { teamSlug, eventSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => createBulkEmail({ path: { teamSlug, eventSlug }, body }));
}
