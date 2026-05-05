import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    deleteEventCustomBulkTemplate,
    getEventCustomBulkTemplate,
    updateEventCustomBulkTemplate,
} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; id: string }> }
) {
    const { teamSlug, eventSlug, id } = await params;
    return callAdmittoApi(() => getEventCustomBulkTemplate({ path: { teamSlug, eventSlug, id } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; id: string }> }
) {
    const { teamSlug, eventSlug, id } = await params;
    const body = await request.json();
    return callAdmittoApi(() =>
        updateEventCustomBulkTemplate({ path: { teamSlug, eventSlug, id }, body })
    );
}

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string; id: string }> }
) {
    const { teamSlug, eventSlug, id } = await params;
    return callAdmittoApi(() => deleteEventCustomBulkTemplate({ path: { teamSlug, eventSlug, id } }));
}
