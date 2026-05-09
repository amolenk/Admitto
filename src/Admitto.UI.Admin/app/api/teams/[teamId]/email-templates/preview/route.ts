import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { previewTeamEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { PreviewEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> },
) {
    const { teamId } = await params;
    const body = await request.json() as PreviewEmailTemplateHttpRequest;
    return callAdmittoApi(() => previewTeamEmailTemplate({ path: { teamId }, body }));
}
