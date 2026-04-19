"use client";

import Link from "next/link";
import { useParams, usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { PageLayout } from "@/components/page-layout";

const navItems = [
    { label: "General", href: "" },
    { label: "Registration", href: "/registration" },
    { label: "Email", href: "/email" },
];

export default function EventSettingsLayout({ children }: { children: React.ReactNode }) {
    const params = useParams<{ teamSlug: string; eventSlug: string }>();
    const pathname = usePathname();
    const basePath = `/teams/${params.teamSlug}/events/${params.eventSlug}/settings`;

    return (
        <PageLayout title="Event settings">
            <div className="flex gap-8">
                <nav className="w-48 shrink-0 space-y-1">
                    {navItems.map((item) => {
                        const fullHref = `${basePath}${item.href}`;
                        const isActive = pathname === fullHref;
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
