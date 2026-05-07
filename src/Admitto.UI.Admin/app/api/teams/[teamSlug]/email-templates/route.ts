import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    createTeamEmailTemplate,
    getTeamEmailTemplates,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { CreateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string }> },
) {
    const { teamSlug } = await params;
    return callAdmittoApi(() => getTeamEmailTemplates({ path: { teamSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string }> },
) {
    const { teamSlug } = await params;
    const body = await request.json() as CreateEmailTemplateHttpRequest;
    return callAdmittoApi(() => createTeamEmailTemplate({ path: { teamSlug }, body }));
}
