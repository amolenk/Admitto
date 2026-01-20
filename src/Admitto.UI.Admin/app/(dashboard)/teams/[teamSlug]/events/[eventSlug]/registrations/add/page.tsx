"use client";

import { AddRegistrationForm } from "./add-registration-form";
import { PageLayout } from "@/components/page-layout";

export default function AddRegistrationPage()
{
    return (
        <PageLayout title="Add registration">
            <AddRegistrationForm />
        </PageLayout>
    );
}
