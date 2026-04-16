"use client";

import { PageLayout } from "@/components/page-layout";
import { Construction } from "lucide-react";

export default function AddRegistrationPage()
{
    return (
        <PageLayout title="Add registration">
            <div className="flex flex-col items-center justify-center gap-4 py-16 text-muted-foreground">
                <Construction className="h-12 w-12" />
                <h2 className="text-xl font-semibold">Coming Soon</h2>
                <p className="max-w-md text-center text-sm">
                    Adding registrations through the admin UI is not yet available.
                    This feature is currently being built.
                </p>
            </div>
        </PageLayout>
    );
}
