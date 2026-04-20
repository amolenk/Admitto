"use client";

import { useParams } from "next/navigation";
import { RegistrationPolicyForm } from "./registration-policy-form";

export default function RegistrationSettingsPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();

    return (
        <RegistrationPolicyForm teamSlug={teamSlug} eventSlug={eventSlug} />
    );
}
