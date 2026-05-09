import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getBulkEmail } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; jobId: string }> }
) {
    const { teamId, eventId, jobId } = await params;
    return callAdmittoApi(() =>
        getBulkEmail({ path: { teamId, eventId, bulkEmailJobId: jobId } })
    );
}
