import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { sharedScannerCheckIn } from "@/lib/admitto-api/generated";
import type { SharedScannerCheckInHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ secret: string }> },
) {
    const { secret } = await params;
    const body = (await request.json()) as SharedScannerCheckInHttpRequest;
    return callAdmittoApi(() => sharedScannerCheckIn({ path: { secret }, body }));
}
