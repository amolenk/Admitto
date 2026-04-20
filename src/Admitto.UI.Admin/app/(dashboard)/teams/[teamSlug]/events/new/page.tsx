"use client";

import { useParams } from "next/navigation";
import { useEffect } from "react";
import { useHeader } from "@/components/header-context";
import { useTeams } from "@/hooks/use-teams";
import { CreateEventForm } from "./create-event-form";

export default function NewEventPage() {
    const { teamSlug } = useParams<{ teamSlug: string }>();
    const { selectedTeam } = useTeams();
    const { setTitle, setBreadcrumbs } = useHeader();

    useEffect(() => {
        setTitle("Create event");
        setBreadcrumbs([
            { label: selectedTeam?.name ?? teamSlug, href: `/teams/${teamSlug}/settings` },
            { label: "New event" },
        ]);
        return () => setBreadcrumbs([]);
    }, [setTitle, setBreadcrumbs, teamSlug, selectedTeam]);

    return <CreateEventForm />;
}
