"use client";

import * as React from "react";
import { useParams } from "next/navigation";
import { PageLayout } from "@/components/page-layout";
import { Construction } from "lucide-react";

export default function ViewEventPage()
{
    const { eventSlug } = useParams();

    return (
        <PageLayout title={eventSlug as string}>
            <div className="flex flex-col items-center justify-center gap-4 py-16 text-muted-foreground">
                <Construction className="h-12 w-12" />
                <h2 className="text-xl font-semibold">Coming Soon</h2>
                <p className="max-w-md text-center text-sm">
                    Attendee management and ticket scanning for this event are not yet available.
                    These features are currently being built.
                </p>
            </div>
        </PageLayout>
    );
}
