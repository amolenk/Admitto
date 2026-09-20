import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { resolveSharedScannerSession } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ secret: string }> },
) {
    const { secret } = await params;
    return callAdmittoApi(() => resolveSharedScannerSession({ path: { secret } }));
}
