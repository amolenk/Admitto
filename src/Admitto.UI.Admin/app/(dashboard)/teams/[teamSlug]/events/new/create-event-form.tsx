"use client";

import { useEffect, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { DateTimePicker } from "@/components/ui/date-time-picker";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Spinner } from "@/components/ui/spinner";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";

function Field({ label, hint, children }: {
    label: string;
    hint?: string;
    children: React.ReactNode;
}) {
    return (
        <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4">
            <div>
                <label className="text-[13.5px] font-medium">{label}</label>
                {hint && <p className="text-[12px] text-muted-foreground mt-0.5 leading-snug">{hint}</p>}
            </div>
            <div className="min-w-0">{children}</div>
        </div>
    );
}

const slugRegex = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/;

const createEventSchema = z
    .object({
        slug: z
            .string()
            .min(1, "Slug is required")
            .regex(slugRegex, "Slug must be lowercase letters, digits, or hyphens"),
        name: z.string().min(1, "Name is required"),
        websiteUrl: z.string().url("Must be a valid URL").min(1, "Website URL is required"),
        baseUrl: z.string().url("Must be a valid URL").min(1, "Base URL is required"),
        startsAt: z.string().min(1, "Start date/time is required"),
        endsAt: z.string().min(1, "End date/time is required"),
    })
    .refine((d) => new Date(d.startsAt) < new Date(d.endsAt), {
        path: ["endsAt"],
        message: "End must be after start",
    });

type CreateEventValues = z.infer<typeof createEventSchema>;

type CreateEventAcceptedResponse = {
    creationRequestId: string | null;
    statusUrl: string | null;
};

type EventCreationRequestDto = {
    creationRequestId: string;
    teamId: string;
    requestedSlug: string;
    requesterId: string;
    requestedAt: string;
    status: "Pending" | "Created" | "Rejected" | "Expired";
    completedAt?: string | null;
    ticketedEventId?: string | null;
    rejectionReason?: string | null;
};

// Polling schedule: start at 500ms, exponentially back off to ~2s, give up after 30s.
const POLL_INITIAL_MS = 500;
const POLL_MAX_MS = 2000;
const POLL_BACKOFF = 1.4;
const POLL_MAX_WAIT_MS = 30_000;

function sleep(ms: number) {
    return new Promise<void>((resolve) => setTimeout(resolve, ms));
}

export function CreateEventForm() {
    const router = useRouter();
    const queryClient = useQueryClient();
    const { teamSlug } = useParams<{ teamSlug: string }>();

    const form = useCustomForm<CreateEventValues>(createEventSchema, {
        slug: "",
        name: "",
        websiteUrl: "",
        baseUrl: "",
        startsAt: "",
        endsAt: "",
    });

    const [pollProgress, setPollProgress] = useState(0);
    const [isPolling, setIsPolling] = useState(false);
    const [isNavigating, setIsNavigating] = useState(false);
    const cancelledRef = useRef(false);

    useEffect(() => {
        cancelledRef.current = false;
        return () => {
            cancelledRef.current = true;
        };
    }, []);

    async function pollCreationStatus(creationRequestId: string, slug: string) {
        const startedAt = Date.now();
        let delay = POLL_INITIAL_MS;

        setIsPolling(true);
        setPollProgress(5);
        let succeeded = false;

        try {
            while (!cancelledRef.current) {
                const elapsed = Date.now() - startedAt;
                const pct = Math.min(95, Math.round((elapsed / POLL_MAX_WAIT_MS) * 95) + 5);
                setPollProgress(pct);

                if (elapsed >= POLL_MAX_WAIT_MS) {
                    form.setError("root", {
                        type: "timeout",
                        message: "Creation timed out, please try again.",
                    });
                    return;
                }

                await sleep(delay);
                delay = Math.min(POLL_MAX_MS, Math.round(delay * POLL_BACKOFF));

                let dto: EventCreationRequestDto;
                try {
                    dto = await apiClient.get<EventCreationRequestDto>(
                        `/api/teams/${teamSlug}/event-creations/${creationRequestId}`
                    );
                } catch (err) {
                    // 404 right after accept can happen; keep polling until timeout.
                    if (err instanceof FormError && err.status === 404) {
                        continue;
                    }
                    throw err;
                }

                if (cancelledRef.current) return;

                if (dto.status === "Created") {
                    succeeded = true;
                    setPollProgress(100);
                    setIsNavigating(true);
                    await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
                    router.push(`/teams/${teamSlug}/events/${slug}/settings`);
                    return;
                }

                if (dto.status === "Rejected") {
                    const reason = dto.rejectionReason ?? "unknown";
                    if (reason === "duplicate_slug") {
                        form.setError("slug", {
                            type: "server",
                            message: "An event with this slug already exists for this team.",
                        });
                    } else {
                        form.setError("root", {
                            type: "server",
                            message: `Creation rejected: ${reason}`,
                        });
                    }
                    return;
                }

                if (dto.status === "Expired") {
                    form.setError("root", {
                        type: "expired",
                        message: "Creation timed out, please try again.",
                    });
                    return;
                }
                // Pending: loop again.
            }
        } finally {
            // Keep the busy state on success so the form doesn't flash re-enabled
            // before Next.js finishes navigating away from this page.
            if (!succeeded) {
                setIsPolling(false);
                setPollProgress(0);
            }
        }
    }

    async function onSubmit(values: CreateEventValues) {
        const body = {
            slug: values.slug,
            name: values.name,
            websiteUrl: values.websiteUrl,
            baseUrl: values.baseUrl,
            startsAt: new Date(values.startsAt).toISOString(),
            endsAt: new Date(values.endsAt).toISOString(),
        };

        const accepted = await apiClient.post<CreateEventAcceptedResponse>(
            `/api/teams/${teamSlug}/events`,
            body
        );

        if (!accepted?.creationRequestId) {
            throw new FormError({
                status: 502,
                title: "Unexpected response",
                detail: "Server accepted the request but did not return a creation id.",
                errors: {},
            });
        }

        await pollCreationStatus(accepted.creationRequestId, values.slug);
    }

    const rootError = form.formState.errors.root?.message;
    const busy = form.formState.isSubmitting || isPolling || isNavigating;

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Create event</h2>
                    <p className="text-[13.5px] text-muted-foreground">Set up a new ticketed event for your team.</p>
                </div>
                <Button size="sm" onClick={form.submit(onSubmit)} disabled={busy}>
                    {busy ? <Spinner className="size-3.5" /> : <Check className="size-3.5" />}
                    {form.formState.isSubmitting && !isPolling && !isNavigating
                        ? "Submitting\u2026"
                        : isNavigating
                            ? "Opening\u2026"
                            : isPolling
                                ? "Creating\u2026"
                                : "Create event"}
                </Button>
            </div>

            {form.generalError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{form.generalError.title}</AlertTitle>
                    <AlertDescription>{form.generalError.detail}</AlertDescription>
                </Alert>
            )}

            {rootError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Unable to create event</AlertTitle>
                    <AlertDescription>{rootError}</AlertDescription>
                </Alert>
            )}

            {(isPolling || isNavigating) && (
                <Card className="mb-5 p-4">
                    <div className="flex items-center gap-3 mb-3">
                        <Spinner className="size-4 text-primary" />
                        <div className="text-[13.5px] font-medium">
                            {isNavigating ? "Opening event\u2026" : "Creating your event\u2026"}
                        </div>
                    </div>
                    <Progress value={isNavigating ? 100 : pollProgress} className="h-1.5" />
                    <p className="text-[12px] text-muted-foreground mt-2">
                        This usually takes just a moment. Please don&apos;t close this page.
                    </p>
                </Card>
            )}

            <Form {...form}>
                <fieldset disabled={busy} className="contents">
                    <form onSubmit={form.submit(onSubmit)}>
                        <Card>
                            <div className="px-6 divide-y">
                                <FormField
                                    control={form.control}
                                    name="slug"
                                    render={({ field }) => (
                                        <Field label="Slug" hint="Used in URLs. Cannot be changed later.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input placeholder="e.g. devconf-2026" className="max-w-sm font-mono" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="name"
                                    render={({ field }) => (
                                        <Field label="Event name" hint="Shown on the public page and in all emails.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input placeholder="e.g. DevConf 2026" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="websiteUrl"
                                    render={({ field }) => (
                                        <Field label="Website" hint="Public website for the event.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input type="url" placeholder="https://example.com" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="baseUrl"
                                    render={({ field }) => (
                                        <Field label="Base URL" hint="Base URL for registration links and emails.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input type="url" placeholder="https://register.example.com" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="startsAt"
                                    render={({ field }) => (
                                        <Field label="Starts at" hint="Event start date and time.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <DateTimePicker {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="endsAt"
                                    render={({ field }) => (
                                        <Field label="Ends at" hint="Event end date and time.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <DateTimePicker {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />
                            </div>
                        </Card>
                    </form>
                </fieldset>
            </Form>
        </div>
    );
}
