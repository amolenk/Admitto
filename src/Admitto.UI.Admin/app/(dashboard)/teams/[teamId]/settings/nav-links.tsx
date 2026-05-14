"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { Settings, Users, Trash2, Mail, KeyRound } from "lucide-react";

const navItems = [
    { label: "General", href: "", icon: Settings, desc: "Team name", exact: true },
    { label: "Members", href: "/members", icon: Users, desc: "Roles and access" },
    { label: "Email", href: "/email", icon: Mail, desc: "SMTP and sender identity", exact: true },
    { label: "Email templates", href: "/email/templates", icon: Mail, desc: "Customize email content" },
    { label: "API Keys", href: "/api-keys", icon: KeyRound, desc: "Public API authentication" },
    { label: "Danger zone", href: "/danger", icon: Trash2, desc: "Archive team" },
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
