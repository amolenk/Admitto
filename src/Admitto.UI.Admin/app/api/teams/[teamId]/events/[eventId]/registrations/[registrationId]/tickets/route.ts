import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { changeAttendeeTickets } from "@/lib/admitto-api/generated/sdk.gen";
import type { ChangeAttendeeTicketsHttpRequest } from "@/lib/admitto-api/generated/types.gen";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; registrationId: string }> },
) {
    const { teamId, eventId, registrationId } = await params;
    const body = await request.json() as ChangeAttendeeTicketsHttpRequest;
    return callAdmittoApi(() => changeAttendeeTickets({ path: { teamId, eventId, registrationId }, body }));
}
