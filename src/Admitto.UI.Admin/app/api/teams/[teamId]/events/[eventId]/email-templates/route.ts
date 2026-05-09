import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    createEventEmailTemplate,
    getEventEmailTemplates,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { CreateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> },
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getEventEmailTemplates({ path: { teamId, eventId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> },
) {
    const { teamId, eventId } = await params;
    const body = await request.json() as CreateEmailTemplateHttpRequest;
    return callAdmittoApi(() => createEventEmailTemplate({ path: { teamId, eventId }, body }));
}
