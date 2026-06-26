import { redirect } from "next/navigation";

export default async function EventEmailsPage({
    params,
}: {
    params: Promise<{ teamId: string; eventId: string }>;
}) {
    const { teamId, eventId } = await params;
    redirect(`/teams/${teamId}/events/${eventId}/emails/campaigns`);
}
