import { PageLayout } from "@/components/page-layout";
import { getTeam } from "@/lib/admitto-api/generated/sdk.gen";
import { NavLinks } from "./nav-links";

export default async function SettingsLayout({
    children,
    params,
}: {
    children: React.ReactNode;
    params: Promise<{ teamId: string }>;
}) {
    const { teamId } = await params;
    const result = await getTeam({ path: { teamId } });
    const teamName = result.data?.name ?? "";
    const basePath = `/teams/${teamId}/settings`;

    const breadcrumbs = [
        { label: teamName, href: `/teams/${teamId}/settings` },
        { label: "Settings" },
    ];

    return (
        <PageLayout title="Team settings" breadcrumbs={breadcrumbs}>
            <div className="mb-5">
                <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                    Settings
                </div>
                <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                    {teamName}
                </h1>
            </div>
            <div className="grid grid-cols-12 gap-8">
                <div className="col-span-12 lg:col-span-3">
                    <NavLinks basePath={basePath} />
                </div>
                <div className="col-span-12 lg:col-span-9">{children}</div>
            </div>
        </PageLayout>
    );
}

