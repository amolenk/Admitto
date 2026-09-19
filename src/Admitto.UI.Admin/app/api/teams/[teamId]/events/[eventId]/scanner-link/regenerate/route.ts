import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { regenerateScannerLink } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> },
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => regenerateScannerLink({ path: { teamId, eventId } }));
}
