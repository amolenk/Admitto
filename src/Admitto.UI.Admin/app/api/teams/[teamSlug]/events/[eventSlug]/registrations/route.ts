import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { adminRegisterAttendee, getRegistrations } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    return callAdmittoApi(() => getRegistrations({ path: { teamSlug, eventSlug } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return callAdmittoApi(() => adminRegisterAttendee({ path: { teamSlug, eventSlug }, body }));
}
