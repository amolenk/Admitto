"use client";

import { QueryErrorResetBoundary } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";

export default function DashboardError({
    error,
    reset,
}: {
    error: Error & { digest?: string };
    reset: () => void;
}) {
    return (
        <QueryErrorResetBoundary>
            {({ reset: resetQuery }) => (
                <div className="flex flex-1 items-center justify-center">
                    <div className="flex flex-col items-center gap-4 text-center max-w-md">
                        <AlertCircle className="h-10 w-10 text-destructive" />
                        <h2 className="text-lg font-semibold">Something went wrong</h2>
                        <p className="text-sm text-muted-foreground">
                            {error.message || "An unexpected error occurred."}
                        </p>
                        <Button
                            onClick={() => {
                                resetQuery();
                                reset();
                            }}
                        >
                            Try again
                        </Button>
                    </div>
                </div>
            )}
        </QueryErrorResetBoundary>
    );
}
