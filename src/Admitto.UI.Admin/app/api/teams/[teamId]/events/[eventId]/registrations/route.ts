import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { adminRegisterAttendee, getRegistrations } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    return callAdmittoApi(() => getRegistrations({ path: { teamId, eventId } }));
}

export async function POST(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;
    const body = await request.json();
    return callAdmittoApi(() => adminRegisterAttendee({ path: { teamId, eventId }, body }));
}
