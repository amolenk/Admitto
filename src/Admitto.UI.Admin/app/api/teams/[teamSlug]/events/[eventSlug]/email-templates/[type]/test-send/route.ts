import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { testSendEventEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { TestSendEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; type: string }> },
) {
    const { teamSlug, eventSlug, type } = await params;
    const body = await request.json() as TestSendEmailTemplateHttpRequest;
    return callAdmittoApi(() => testSendEventEmailTemplate({ path: { teamSlug, eventSlug, type }, body }));
}
