import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { previewBulkEmail } from "@/lib/admitto-api/generated";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => previewBulkEmail({ path: { teamSlug, eventSlug }, body }));
}
