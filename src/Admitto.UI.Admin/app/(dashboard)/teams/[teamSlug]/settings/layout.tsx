"use client";

import Link from "next/link";
import { useParams, usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { PageLayout } from "@/components/page-layout";

const navItems = [
    { label: "General", href: "", enabled: true },
    { label: "Members", href: "/members", enabled: true },
    { label: "Danger Zone", href: "/danger", enabled: true },
];

export default function SettingsLayout({ children }: { children: React.ReactNode }) {
    const params = useParams<{ teamSlug: string }>();
    const pathname = usePathname();
    const basePath = `/teams/${params.teamSlug}/settings`;

    return (
        <PageLayout title="Team settings">
            <div className="flex gap-8">
                <nav className="w-48 shrink-0 space-y-1">
                    {navItems.map((item) => {
                        const fullHref = `${basePath}${item.href}`;
                        const isActive = pathname === fullHref;

                        if (!item.enabled) {
                            return (
                                <div
                                    key={item.label}
                                    className="block rounded-md px-3 py-2 text-sm text-muted-foreground cursor-not-allowed opacity-50"
                                >
                                    {item.label}
                                </div>
                            );
                        }

                        return (
                            <Link
                                key={item.label}
                                href={fullHref}
                                className={cn(
                                    "block rounded-md px-3 py-2 text-sm font-medium transition-colors",
                                    isActive
                                        ? "bg-muted text-foreground"
                                        : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                                )}
                            >
                                {item.label}
                            </Link>
                        );
                    })}
                </nav>
                <div className="flex-1 min-w-0">{children}</div>
            </div>
        </PageLayout>
    );
}
