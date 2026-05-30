import { PageLayout } from "@/components/page-layout";
import { getTeam, getTicketedEventDetails } from "@/lib/admitto-api/generated/sdk.gen";
import { NavLinks } from "./nav-links";

export default async function EventSettingsLayout({
    children,
    params,
}: {
    children: React.ReactNode;
    params: Promise<{ teamId: string; eventId: string }>;
}) {
    const { teamId, eventId } = await params;

    const [teamResult, eventResult] = await Promise.all([
        getTeam({ path: { teamId } }),
        getTicketedEventDetails({ path: { teamId, eventId } }),
    ]);

    const teamName = teamResult.data?.name ?? "";
    const eventName = eventResult.data?.name ?? "";
    const basePath = `/teams/${teamId}/events/${eventId}/settings`;

    const breadcrumbs = [
        { label: teamName, href: `/teams/${teamId}/settings` },
        { label: eventName, href: `/teams/${teamId}/events/${eventId}` },
        { label: "Settings" },
    ];

    return (
        <PageLayout title="Event settings" breadcrumbs={breadcrumbs}>
            <div className="mb-5">
                <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                    Settings
                </div>
                <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                    {eventName}
                </h1>
            </div>
            <div className="grid grid-cols-12 gap-4 lg:gap-8">
                <div className="col-span-12 lg:col-span-3">
                    <NavLinks basePath={basePath} />
                </div>
                <div className="col-span-12 lg:col-span-9">{children}</div>
            </div>
        </PageLayout>
    );
}

