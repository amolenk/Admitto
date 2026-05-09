import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteEventEmailTemplate,
    getEventEmailTemplate,
    updateEventEmailTemplate,
} from "@/lib/admitto-api/generated/sdk.gen";
import type { UpdateEmailTemplateHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; id: string }> },
) {
    const { teamId, eventId, id } = await params;
    return callAdmittoApi(() => getEventEmailTemplate({ path: { teamId, eventId, id } as { id: string } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; id: string }> },
) {
    const { teamId, eventId, id } = await params;
    const body = await request.json() as UpdateEmailTemplateHttpRequest;
    return callAdmittoApi(() => updateEventEmailTemplate({ path: { teamId, eventId, id } as { id: string }, body }));
}

export async function DELETE(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; id: string }> },
) {
    const { teamId, eventId, id } = await params;
    const url = new URL(request.url);
    const version = Number(url.searchParams.get("version"));
    return callAdmittoApi(() => deleteEventEmailTemplate({ path: { teamId, eventId, id } as { id: string }, query: { version } }));
}
