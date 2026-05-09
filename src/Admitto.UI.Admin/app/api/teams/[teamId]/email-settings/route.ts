import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { deleteTeamEmailSettings, getTeamEmailSettings, upsertTeamEmailSettings } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    return callAdmittoApi(() => getTeamEmailSettings({ path: { teamId } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => upsertTeamEmailSettings({ path: { teamId }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> }
) {
    const { teamId } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteTeamEmailSettings({ path: { teamId }, query: { version } }));
}
