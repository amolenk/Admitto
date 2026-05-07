import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteEventEmailTemplate,
    getEventEmailTemplate,
    updateEventEmailTemplate,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { UpdateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; id: string }> },
) {
    const { teamSlug, eventSlug, id } = await params;
    return callAdmittoApi(() => getEventEmailTemplate({ path: { teamSlug, eventSlug, id } as { id: string } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; id: string }> },
) {
    const { teamSlug, eventSlug, id } = await params;
    const body = await request.json() as UpdateEmailTemplateHttpRequest;
    return callAdmittoApi(() => updateEventEmailTemplate({ path: { teamSlug, eventSlug, id } as { id: string }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; id: string }> },
) {
    const { teamSlug, eventSlug, id } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteEventEmailTemplate({ path: { teamSlug, eventSlug, id } as { id: string }, query: { version } }));
}
