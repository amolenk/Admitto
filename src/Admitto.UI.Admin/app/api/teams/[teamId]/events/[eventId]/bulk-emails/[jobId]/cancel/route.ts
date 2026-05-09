import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { cancelBulkEmail } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; jobId: string }> }
) {
    const { teamId, eventId, jobId } = await params;
    return callAdmittoApi(() =>
        cancelBulkEmail({ path: { teamId, eventId, bulkEmailJobId: jobId } })
    );
}
