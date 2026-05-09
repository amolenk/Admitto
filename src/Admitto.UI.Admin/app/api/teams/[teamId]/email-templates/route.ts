import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    createTeamEmailTemplate,
    getTeamEmailTemplates,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { CreateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string }> },
) {
    const { teamId } = await params;
    return callAdmittoApi(() => getTeamEmailTemplates({ path: { teamId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string }> },
) {
    const { teamId } = await params;
    const body = await request.json() as CreateEmailTemplateHttpRequest;
    return callAdmittoApi(() => createTeamEmailTemplate({ path: { teamId }, body }));
}
