import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { revokeScannerLink } from "@/lib/admitto-api/generated";

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> },
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => revokeScannerLink({ path: { teamId, eventId } }));
}
