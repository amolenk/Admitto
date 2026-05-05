import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteEventEmailTemplate,
    getEventEmailTemplate,
    upsertEventEmailTemplate,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { UpsertEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; type: string }> },
) {
    const { teamSlug, eventSlug, type } = await params;
    return callAdmittoApi(() => getEventEmailTemplate({ path: { teamSlug, eventSlug, type } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; type: string }> },
) {
    const { teamSlug, eventSlug, type } = await params;
    const body = await request.json() as UpsertEmailTemplateHttpRequest;
    return callAdmittoApi(() => upsertEventEmailTemplate({ path: { teamSlug, eventSlug, type }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; type: string }> },
) {
    const { teamSlug, eventSlug, type } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteEventEmailTemplate({ path: { teamSlug, eventSlug, type }, query: { version } }));
}
