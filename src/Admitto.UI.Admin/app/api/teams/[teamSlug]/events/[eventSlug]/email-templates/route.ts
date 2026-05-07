import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    createEventEmailTemplate,
    getEventEmailTemplates,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { CreateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> },
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getEventEmailTemplates({ path: { teamSlug, eventSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> },
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json() as CreateEmailTemplateHttpRequest;
    return callAdmittoApi(() => createEventEmailTemplate({ path: { teamSlug, eventSlug }, body }));
}
