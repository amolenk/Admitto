"use client";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import { normalizeStatus } from "./event-detail-types";

export function EventStatusBanner({ status }: { status: string }) {
    const normalized = normalizeStatus(status);

    if (normalized === "active") {
        return null;
    }

    const isCancelled = normalized === "cancelled";

    return (
        <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>{isCancelled ? "Event cancelled" : "Event archived"}</AlertTitle>
            <AlertDescription>
                {isCancelled
                    ? "This event has been cancelled. Policies are read-only and cannot be modified."
                    : "This event has been archived. Policies are read-only and cannot be modified."}
            </AlertDescription>
        </Alert>
    );
}
