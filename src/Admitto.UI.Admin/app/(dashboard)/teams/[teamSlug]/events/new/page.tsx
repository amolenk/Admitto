"use client";

import { CreateEventForm } from "./create-event-form";
import { PageLayout } from "@/components/page-layout";

export default function NewEventPage() {
    return (
        <PageLayout title="Create event">
            <CreateEventForm />
        </PageLayout>
    );
}
