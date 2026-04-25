import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { deleteEventEmailSettings, getEventEmailSettings, upsertEventEmailSettings } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getEventEmailSettings({ path: { teamSlug, eventSlug } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => upsertEventEmailSettings({ path: { teamSlug, eventSlug }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteEventEmailSettings({ path: { teamSlug, eventSlug }, query: { version } }));
}
