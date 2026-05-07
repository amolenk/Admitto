import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { previewTeamEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { PreviewEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> },
) {
    const { teamSlug } = await params;
    const body = await request.json() as PreviewEmailTemplateHttpRequest;
    return callAdmittoApi(() => previewTeamEmailTemplate({ path: { teamSlug }, body }));
}
