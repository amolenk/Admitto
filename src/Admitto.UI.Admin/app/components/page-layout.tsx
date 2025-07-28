"use client";

import { useEffect } from "react";
import { useHeader } from "@/components/header-context";

interface PageLayoutProps
{
    title: string;
    children: React.ReactNode;
}

export function PageLayout({ title, children }: PageLayoutProps)
{
    const { setTitle } = useHeader();

    useEffect(() =>
    {
        setTitle(title);
    }, [setTitle, title]);

    return <div className="space-y-6">{children}</div>;
}