"use client";

import { useEffect } from "react";
import { useHeader, BreadcrumbItem } from "@/components/header-context";

interface PageLayoutProps
{
    title: string;
    breadcrumbs?: BreadcrumbItem[];
    children: React.ReactNode;
}

export function PageLayout({ title, breadcrumbs, children }: PageLayoutProps)
{
    const { setTitle, setBreadcrumbs } = useHeader();

    useEffect(() =>
    {
        setTitle(title);
        setBreadcrumbs(breadcrumbs ?? []);
        return () => setBreadcrumbs([]);
    }, [setTitle, setBreadcrumbs, title, breadcrumbs]);

    return <div className="space-y-6">{children}</div>;
}
