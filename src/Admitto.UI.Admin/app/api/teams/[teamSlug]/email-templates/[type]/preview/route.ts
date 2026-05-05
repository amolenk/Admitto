import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { previewTeamEmailTemplate, previewDraftTeamEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { PreviewDraftEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; type: string }> },
) {
    const { teamSlug, type } = await params;
    return callAdmittoApi(() => previewTeamEmailTemplate({ path: { teamSlug, type } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; type: string }> },
) {
    const { teamSlug, type } = await params;
    const body = await request.json() as PreviewDraftEmailTemplateHttpRequest;
    return callAdmittoApi(() => previewDraftTeamEmailTemplate({ path: { teamSlug, type }, body }));
}
