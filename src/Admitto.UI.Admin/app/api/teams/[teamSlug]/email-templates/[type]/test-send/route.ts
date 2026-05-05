import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { testSendTeamEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { TestSendEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; type: string }> },
) {
    const { teamSlug, type } = await params;
    const body = await request.json() as TestSendEmailTemplateHttpRequest;
    return callAdmittoApi(() => testSendTeamEmailTemplate({ path: { teamSlug, type }, body }));
}
