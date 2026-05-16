"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import { normalizeStatus } from "./event-detail-types";

export function EventStatusBanner({ status }: { status: string }) {
    const normalized = normalizeStatus(status);

    if (normalized !== "archived") {
        return null;
    }

    return (
        <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Event archived</AlertTitle>
            <AlertDescription>
                This event has been archived. Policies are read-only and cannot be modified.
            </AlertDescription>
        </Alert>
    );
}
