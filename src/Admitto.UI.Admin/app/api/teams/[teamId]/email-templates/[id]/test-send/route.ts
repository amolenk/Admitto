import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { testSendTeamEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { TestSendEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; id: string }> },
) {
    const { teamId, id } = await params;
    const body = await request.json() as TestSendEmailTemplateHttpRequest;
    return callAdmittoApi(() => testSendTeamEmailTemplate({ path: { teamId, id }, body }));
}
