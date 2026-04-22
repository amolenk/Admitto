import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { configureReconfirmPolicy } from "@/lib/admitto-api/generated";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => configureReconfirmPolicy({ path: { teamSlug, eventSlug }, body }));
}
