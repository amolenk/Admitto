import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { getCancellationPolicy, setCancellationPolicy, removeCancellationPolicy } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getCancellationPolicy({ path: { teamSlug, eventSlug } }));
}

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => setCancellationPolicy({ path: { teamSlug, eventSlug }, body }));
}

export async function DELETE(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => removeCancellationPolicy({ path: { teamSlug, eventSlug } }));
}
