"use client";

import * as React from "react";
import { useEffect } from "react";
import { useParams, useRouter } from "next/navigation";

export default function ViewEventPage()
{
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const router = useRouter();

    useEffect(() =>
    {
        router.replace(`/teams/${teamSlug}/events/${eventSlug}/settings`);
    }, [router, teamSlug, eventSlug]);

    return null;
}
