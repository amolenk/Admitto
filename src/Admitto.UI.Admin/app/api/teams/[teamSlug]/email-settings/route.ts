import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { deleteTeamEmailSettings, getTeamEmailSettings, upsertTeamEmailSettings } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    return callAdmittoApi(() => getTeamEmailSettings({ path: { teamSlug } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => upsertTeamEmailSettings({ path: { teamSlug }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteTeamEmailSettings({ path: { teamSlug }, query: { version } }));
}
