import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { previewEventEmailTemplate } from "@/lib/admitto-api/generated/sdk.gen";
import type { PreviewEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> },
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json() as PreviewEmailTemplateHttpRequest;
    return callAdmittoApi(() => previewEventEmailTemplate({ path: { teamSlug, eventSlug }, body }));
}
