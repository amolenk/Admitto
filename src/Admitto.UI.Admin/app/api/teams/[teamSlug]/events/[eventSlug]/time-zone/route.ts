import { proxyAdmittoApi } from "@/lib/admitto-api/admitto-client";

export async function PUT(
    request: Request,
    { params }: { params: Promise<{ teamSlug: string; eventSlug: string }> }
) {
    const { teamSlug, eventSlug } = await params;
    const body = await request.json();
    return proxyAdmittoApi(
        "PUT",
        `/admin/teams/${encodeURIComponent(teamSlug)}/events/${encodeURIComponent(eventSlug)}/time-zone`,
        body,
    );
}
