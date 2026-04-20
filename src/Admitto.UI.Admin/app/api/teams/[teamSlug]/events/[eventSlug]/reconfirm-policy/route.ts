import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getReconfirmPolicy, setReconfirmPolicy, removeReconfirmPolicy } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getReconfirmPolicy({ path: { teamSlug, eventSlug } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => setReconfirmPolicy({ path: { teamSlug, eventSlug }, body }));
}

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => removeReconfirmPolicy({ path: { teamSlug, eventSlug } }));
}
