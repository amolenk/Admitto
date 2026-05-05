import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteTeamEmailTemplate,
    getTeamEmailTemplate,
    upsertTeamEmailTemplate,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { UpsertEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; type: string }> },
) {
    const { teamSlug, type } = await params;
    return callAdmittoApi(() => getTeamEmailTemplate({ path: { teamSlug, type } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; type: string }> },
) {
    const { teamSlug, type } = await params;
    const body = await request.json() as UpsertEmailTemplateHttpRequest;
    return callAdmittoApi(() => upsertTeamEmailTemplate({ path: { teamSlug, type }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; type: string }> },
) {
    const { teamSlug, type } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteTeamEmailTemplate({ path: { teamSlug, type }, query: { version } }));
}
