import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { deleteEventEmailSettings, getEventEmailSettings, upsertEventEmailSettings } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getEventEmailSettings({ path: { teamId, eventId } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => upsertEventEmailSettings({ path: { teamId, eventId }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteEventEmailSettings({ path: { teamId, eventId }, query: { version } }));
}
