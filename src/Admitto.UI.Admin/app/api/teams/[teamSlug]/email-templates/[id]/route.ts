import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteTeamEmailTemplate,
    getTeamEmailTemplate,
    updateTeamEmailTemplate,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { UpdateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; id: string }> },
) {
    const { teamSlug, id } = await params;
    return callAdmittoApi(() => getTeamEmailTemplate({ path: { teamSlug, id } as { id: string } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; id: string }> },
) {
    const { teamSlug, id } = await params;
    const body = await request.json() as UpdateEmailTemplateHttpRequest;
    return callAdmittoApi(() => updateTeamEmailTemplate({ path: { teamSlug, id } as { id: string }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; id: string }> },
) {
    const { teamSlug, id } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteTeamEmailTemplate({ path: { teamSlug, id } as { id: string }, query: { version } }));
}
