import { checkIn } from "@/lib/admitto-api/generated/sdk.gen";
import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import type { CheckInHttpRequest } from "@/lib/admitto-api/generated/types.gen";
export async function POST(request: Request, { params }: { params: Promise<{ teamId: string; eventId: string }> }) {
    const { teamId, eventId } = await params;
    const body = await request.json() as CheckInHttpRequest;
    return callAdmittoApi(() => checkIn({ path: { teamId, eventId }, body }));
}
