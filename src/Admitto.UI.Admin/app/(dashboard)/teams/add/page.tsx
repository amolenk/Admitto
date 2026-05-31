"use client";

import { CreateTeamForm } from "./create-team-form";
import { PageLayout } from "@/components/page-layout";

export default function CreateTeamPage() {
    return (
        <PageLayout>
            <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight">Create team</h1>
            <CreateTeamForm />
        </PageLayout>
    );
}
