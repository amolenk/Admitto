import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelBulkEmail } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; jobId: string }> }
) {
    const { teamSlug, eventSlug, jobId } = await params;
    return callAdmittoApi(() =>
        cancelBulkEmail({ path: { teamSlug, eventSlug, bulkEmailJobId: jobId } })
    );
}
