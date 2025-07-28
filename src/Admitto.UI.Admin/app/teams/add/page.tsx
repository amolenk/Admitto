"use client";

import { AddTeamForm } from "@/teams/add/add-team-form";
import { PageLayout } from "@/components/page-layout";

export default function AddTeamPage()
{
    return (
        <PageLayout title="Add team">
            <AddTeamForm />
        </PageLayout>
    );
}
