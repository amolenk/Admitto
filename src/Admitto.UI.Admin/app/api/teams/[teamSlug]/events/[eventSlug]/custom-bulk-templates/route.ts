import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import {
    createEventCustomBulkTemplate,
    getEventCustomBulkTemplates,
} from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getEventCustomBulkTemplates({ path: { teamSlug, eventSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => createEventCustomBulkTemplate({ path: { teamSlug, eventSlug }, body }));
}
