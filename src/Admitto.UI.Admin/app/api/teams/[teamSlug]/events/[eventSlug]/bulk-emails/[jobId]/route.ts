import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getBulkEmail } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; jobId: string }> }
) {
    const { teamSlug, eventSlug, jobId } = await params;
    return callAdmittoApi(() =>
        getBulkEmail({ path: { teamSlug, eventSlug, bulkEmailJobId: jobId } })
    );
}
