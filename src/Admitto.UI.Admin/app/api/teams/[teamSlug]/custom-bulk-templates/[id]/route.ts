import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteTeamCustomBulkTemplate,
    getTeamCustomBulkTemplate,
    updateTeamCustomBulkTemplate,
} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; id: string }> }
) {
    const { teamSlug, id } = await params;
    return callAdmittoApi(() => getTeamCustomBulkTemplate({ path: { teamSlug, id } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; id: string }> }
) {
    const { teamSlug, id } = await params;
    const body = await request.json();
    return callAdmittoApi(() => updateTeamCustomBulkTemplate({ path: { teamSlug, id }, body }));
}

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; id: string }> }
) {
    const { teamSlug, id } = await params;
    return callAdmittoApi(() => deleteTeamCustomBulkTemplate({ path: { teamSlug, id } }));
}
