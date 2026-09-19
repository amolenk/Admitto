import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { createScannerLink, getScannerLink } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> },
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getScannerLink({ path: { teamId, eventId } }));
}

export async function POST(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> },
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => createScannerLink({ path: { teamId, eventId } }));
}
