import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    createTeamCustomBulkTemplate,
    getTeamCustomBulkTemplates,
} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    return callAdmittoApi(() => getTeamCustomBulkTemplates({ path: { teamSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> }
) {
    const { teamSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => createTeamCustomBulkTemplate({ path: { teamSlug }, body }));
}
