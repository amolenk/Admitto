"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { Settings, Users, Mail, Zap, Trash2 } from "lucide-react";

const navItems = [
    { label: "General", href: "", icon: Settings, desc: "Name, date, venue, website", exact: true },
    { label: "Registration", href: "/registration", icon: Users, desc: "Policy, windows, waitlist" },
    { label: "Cancellation", href: "/cancellation", icon: Zap, desc: "Late cancellation cutoff" },
    { label: "Reconfirmation", href: "/reconfirm", icon: Mail, desc: "Window and cadence" },
    { label: "Email", href: "/email", icon: Mail, desc: "Templates, SMTP, sender", exact: true },
    { label: "Email templates", href: "/email/templates", icon: Mail, desc: "Customize email content" },
    { label: "Danger zone", href: "/danger", icon: Trash2, desc: "Cancel or archive" },
];

interface NavLinksProps {
    basePath: string;
}

export function NavLinks({ basePath }: NavLinksProps) {
    const pathname = usePathname();

    return (
        <nav className="flex flex-col gap-1">
            {navItems.map((item) => {
                const fullHref = `${basePath}${item.href}`;
                const isActive = item.exact
                    ? pathname === fullHref
                    : pathname === fullHref || pathname.startsWith(`${fullHref}/`);
                const Icon = item.icon;
                return (
                    <Link
                        key={item.label}
                        href={fullHref}
                        className={cn(
                            "flex flex-col items-start rounded-md px-3 py-2.5 text-sm transition-colors border border-transparent",
                            isActive
                                ? "bg-card text-foreground border-border shadow-sm"
                                : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                        )}
                    >
                        <div className="flex items-center gap-2 w-full">
                            <Icon className={cn("size-3.5", isActive ? "text-primary" : "text-muted-foreground")} />
                            <span className="font-medium">{item.label}</span>
                            {isActive && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-primary" />}
                        </div>
                        <div className="text-[11.5px] text-muted-foreground pl-6">{item.desc}</div>
                    </Link>
                );
            })}
        </nav>
    );
}
