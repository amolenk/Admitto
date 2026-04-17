"use client";

import { CreateTeamForm } from "./create-team-form";
import { PageLayout } from "@/components/page-layout";

export default function CreateTeamPage() {
    return (
        <PageLayout title="Add team">
            <CreateTeamForm />
        </PageLayout>
    );
}
