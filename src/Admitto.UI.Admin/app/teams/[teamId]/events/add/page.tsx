"use client";

import { AddEventForm } from "./add-event-form";
import { PageLayout } from "@/components/page-layout";

export default function AddEventPage()
{
    return (
        <PageLayout title="Add event">
            <AddEventForm />
        </PageLayout>
    );
}
