import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { testSendEventEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { TestSendEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; id: string }> },
) {
    const { teamId, eventId, id } = await params;
    const body = await request.json() as TestSendEmailTemplateHttpRequest;
    return callAdmittoApi(() => testSendEventEmailTemplate({ path: { teamId, eventId, id }, body }));
}
