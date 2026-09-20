import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { sharedScannerLookup } from "@/lib/admitto-api/generated";

export async function GET(
    request: Request,
    { params }: { params: Promise<{ secret: string }> },
) {
    const { secret } = await params;
    const query = new URL(request.url).searchParams.get("query") ?? undefined;
    return callAdmittoApi(() => sharedScannerLookup({ path: { secret }, query: { query } }));
}
