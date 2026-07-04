import {callAdmittoApi} from "@/lib/admitto-api/admitto-client";
import {client} from "@/lib/admitto-api/generated/client.gen";

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamId: string; creationRequestId: string }> }) {

    const {teamId, creationRequestId} = await params;

    return callAdmittoApi(() =>
        client.get({
            security: [{scheme: "bearer", type: "http"}],
            url: "/admin/teams/{teamId}/event-creations/{creationRequestId}",
            path: {teamId, creationRequestId},
        })
    );
}
