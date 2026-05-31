"use client";

import { CreateEventForm } from "./create-event-form";

export default function NewEventPage() {
    return (
        <div className="space-y-6">
            <h1 className="font-display text-[22px] font-semibold">Create event</h1>
            <CreateEventForm />
        </div>
    );
}
