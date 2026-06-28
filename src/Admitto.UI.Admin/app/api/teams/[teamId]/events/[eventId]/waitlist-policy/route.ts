import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { configureWaitlistPolicy } from "@/lib/admitto-api/generated";
import type { ConfigureWaitlistPolicyHttpRequest } from "@/lib/admitto-api/generated";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json() as ConfigureWaitlistPolicyHttpRequest;
    return callAdmittoApi(() => configureWaitlistPolicy({ path: { teamId, eventId }, body }));
}
