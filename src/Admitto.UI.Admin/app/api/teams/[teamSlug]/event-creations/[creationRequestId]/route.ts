import {callAdmittoApi} from "@/lib/admitto-api/admitto-client";
import {client} from "@/lib/admitto-api/generated/client.gen";

export async function GET(
    _request: Request,
    {params}: { params: Promise<{ teamSlug: string; creationRequestId: string }> }) {

    const {teamSlug, creationRequestId} = await params;

    return callAdmittoApi(() =>
        client.get({
            security: [{scheme: "bearer", type: "http"}],
            url: "/admin/teams/{teamSlug}/event-creations/{creationRequestId}",
            path: {teamSlug, creationRequestId},
        })
    );
}
