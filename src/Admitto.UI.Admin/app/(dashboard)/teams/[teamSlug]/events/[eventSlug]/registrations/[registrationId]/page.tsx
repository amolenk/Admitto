"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    ArrowLeft,
    CheckCircle,
    Mail,
    Sparkles,
    Trash2,
    Eye,
    RotateCcw,
} from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    AlertDialog,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";

// ── Local types (new DTOs not yet in generated SDK) ──────────────────────────

interface ActivityLogEntryDto {
    activityType: string;
    occurredAt: string;
    metadata?: string | null;
}

interface TicketDetailDto {
    slug: string;
    name: string;
}

interface RegistrationDetailDto {
    id: string;
    email: string;
    firstName?: string | null;
    lastName?: string | null;
    status: string;
    registeredAt: string;
    hasReconfirmed: boolean;
    reconfirmedAt?: string | null;
    cancellationReason?: string | null;
    tickets: TicketDetailDto[];
    additionalDetails: Record<string, string>;
    activities: ActivityLogEntryDto[];
}

interface AttendeeEmailLogItemDto {
    id: string;
    subject: string;
    emailType: string;
    status: string;
    sentAt?: string | null;
    bulkEmailJobId?: string | null;
}

// ── Fetch helpers ─────────────────────────────────────────────────────────────

async function fetchRegistrationDetail(
    teamSlug: string,
    eventSlug: string,
    registrationId: string,
): Promise<RegistrationDetailDto> {
    return apiClient.get<RegistrationDetailDto>(
        `/api/teams/${teamSlug}/events/${eventSlug}/registrations/${registrationId}`,
    );
}

async function fetchAttendeeEmails(
    teamSlug: string,
    eventSlug: string,
    registrationId: string,
): Promise<AttendeeEmailLogItemDto[]> {
    return apiClient.get<AttendeeEmailLogItemDto[]>(
        `/api/teams/${teamSlug}/events/${eventSlug}/registrations/${registrationId}/emails`,
    );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function attendeeFullName(r: RegistrationDetailDto): string {
    const full = [r.firstName, r.lastName].filter(Boolean).join(" ").trim();
    if (full) return full;
    const at = r.email.indexOf("@");
    return at > 0 ? r.email.slice(0, at) : r.email;
}

function initials(name: string): string {
    return name
        .split(" ")
        .map((s) => s[0])
        .slice(0, 2)
        .join("")
        .toUpperCase();
}

function formatTs(iso: string): string {
    return new Date(iso).toLocaleString(undefined, { hour12: false });
}

function formatRelative(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return "just now";
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
}

function cancellationReasonLabel(reason?: string | null): string {
    if (reason === "AttendeeRequest") return "Attendee request";
    if (reason === "VisaLetterDenied") return "Visa letter denied";
    if (reason === "TicketTypesRemoved") return "Ticket types removed";
    return reason ?? "Unknown reason";
}

// ── Timeline item definition ──────────────────────────────────────────────────

type TimelineKind = "registered" | "reconfirmed" | "cancelled" | "email";

interface TimelineEntry {
    kind: TimelineKind;
    ts: string; // ISO for sorting
    title: string;
    detail: string;
    emailItem?: AttendeeEmailLogItemDto;
}

function buildTimeline(
    activities: ActivityLogEntryDto[],
    emails: AttendeeEmailLogItemDto[],
): TimelineEntry[] {
    const activityEntries: TimelineEntry[] = activities.map((a) => {
        const kind = a.activityType.toLowerCase() as TimelineKind;
        let title = a.activityType;
        let detail = "";
        if (kind === "registered") {
            title = "Started registration";
            detail = "Attendee registered for the event.";
        } else if (kind === "reconfirmed") {
            title = "Attendance reconfirmed";
            detail = "Attendee confirmed their attendance.";
        } else if (kind === "cancelled") {
            const reason = cancellationReasonLabel(a.metadata);
            title = `Registration cancelled (${reason})`;
            detail = "Registration was cancelled.";
        }
        return { kind, ts: a.occurredAt, title, detail };
    });

    const emailEntries: TimelineEntry[] = emails.map((e) => ({
        kind: "email" as TimelineKind,
        ts: e.sentAt ?? e.sentAt ?? new Date(0).toISOString(),
        title: e.subject,
        detail: `Type: ${e.emailType} · Status: ${e.status}`,
        emailItem: e,
    }));

    return [...activityEntries, ...emailEntries].sort(
        (a, b) => new Date(b.ts).getTime() - new Date(a.ts).getTime(),
    );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function AttendeeDetailPage() {
    const { teamSlug, eventSlug, registrationId } = useParams<{
        teamSlug: string;
        eventSlug: string;
        registrationId: string;
    }>();
    const { selectedTeam } = useTeams();
    const queryClient = useQueryClient();

    const detailQuery = useQuery({
        queryKey: ["registration-detail", teamSlug, eventSlug, registrationId],
        queryFn: () => fetchRegistrationDetail(teamSlug, eventSlug, registrationId),
        throwOnError: false,
        retry: false,
    });

    const emailsQuery = useQuery({
        queryKey: ["attendee-emails", teamSlug, eventSlug, registrationId],
        queryFn: () => fetchAttendeeEmails(teamSlug, eventSlug, registrationId),
        throwOnError: false,
        retry: false,
    });

    const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
    const [cancelReason, setCancelReason] = useState("");
    const [isCancelling, setIsCancelling] = useState(false);

    const [timelineFilter, setTimelineFilter] = useState<"all" | "events" | "emails">("all");

    const registration = detailQuery.data;
    const emails = emailsQuery.data ?? [];
    const isLoading = detailQuery.isLoading || emailsQuery.isLoading;
    const hasError = detailQuery.isError || emailsQuery.isError;

    const timeline = useMemo(
        () => buildTimeline(registration?.activities ?? [], emails),
        [registration, emails],
    );

    const visibleTimeline = useMemo(() => {
        if (timelineFilter === "events") return timeline.filter((e) => e.kind !== "email");
        if (timelineFilter === "emails") return timeline.filter((e) => e.kind === "email");
        return timeline;
    }, [timeline, timelineFilter]);

    async function handleCancelConfirm() {
        if (!cancelReason) return;
        setIsCancelling(true);
        try {
            await apiClient.post(
                `/api/teams/${teamSlug}/events/${eventSlug}/registrations/${registrationId}/cancel`,
                { reason: cancelReason },
            );
            await queryClient.invalidateQueries({
                queryKey: ["registration-detail", teamSlug, eventSlug, registrationId],
            });
            toast.success("Registration cancelled successfully.");
            setCancelDialogOpen(false);
            setCancelReason("");
        } catch {
            toast.error("Failed to cancel registration. Please try again.");
        } finally {
            setIsCancelling(false);
        }
    }

    const name = registration ? attendeeFullName(registration) : "";
    const breadcrumbs = [
        { label: selectedTeam?.name ?? teamSlug, href: `/teams/${teamSlug}/settings` },
        { label: eventSlug, href: `/teams/${teamSlug}/events/${eventSlug}` },
        { label: "Registrations", href: `/teams/${teamSlug}/events/${eventSlug}/registrations` },
        { label: name || registrationId },
    ];

    return (
        <PageLayout title="Attendee" breadcrumbs={breadcrumbs}>
            {/* Back link */}
            <div className="flex items-center gap-2 text-[13px]">
                <Button variant="ghost" size="sm" asChild className="text-muted-foreground">
                    <Link href={`/teams/${teamSlug}/events/${eventSlug}/registrations`}>
                        <ArrowLeft className="size-3.5" /> Registrations
                    </Link>
                </Button>
                {name && (
                    <>
                        <span className="text-muted-foreground">/</span>
                        <span className="text-muted-foreground">{name}</span>
                    </>
                )}
            </div>

            {/* Error state */}
            {hasError && !isLoading && (
                <Card className="p-8 text-center text-sm text-muted-foreground">
                    Failed to load attendee details. Please refresh and try again.
                </Card>
            )}

            {/* Loading skeletons */}
            {isLoading && (
                <>
                    <Card className="p-6">
                        <div className="flex items-start gap-4">
                            <Skeleton className="h-14 w-14 rounded-full" />
                            <div className="flex-1 space-y-2">
                                <Skeleton className="h-7 w-56" />
                                <Skeleton className="h-4 w-80" />
                            </div>
                        </div>
                    </Card>
                    <div className="grid grid-cols-12 gap-5">
                        <div className="col-span-12 lg:col-span-5 flex flex-col gap-5">
                            <Card className="p-5 space-y-3">
                                <Skeleton className="h-5 w-32" />
                                {[...Array(5)].map((_, i) => (
                                    <Skeleton key={i} className="h-4 w-full" />
                                ))}
                            </Card>
                            <Card className="p-5 space-y-3">
                                <Skeleton className="h-5 w-24" />
                                <Skeleton className="h-20 w-full" />
                            </Card>
                        </div>
                        <div className="col-span-12 lg:col-span-7">
                            <Card className="p-5 space-y-4">
                                <Skeleton className="h-5 w-40" />
                                {[...Array(4)].map((_, i) => (
                                    <Skeleton key={i} className="h-12 w-full" />
                                ))}
                            </Card>
                        </div>
                    </div>
                </>
            )}

            {/* Main content */}
            {!isLoading && !hasError && registration && (
                <>
                    {/* Hero card */}
                    <Card className="overflow-hidden">
                        <div className="p-6 flex items-start justify-between gap-6 flex-wrap">
                            <div className="flex items-center gap-4 min-w-0">
                                <div className="h-14 w-14 rounded-full bg-muted grid place-items-center text-[16px] font-semibold text-muted-foreground flex-none">
                                    {initials(name)}
                                </div>
                                <div className="min-w-0">
                                    <div className="flex items-center gap-2 flex-wrap mb-1">
                                        <h1 className="font-display text-[26px] font-semibold tracking-tight leading-none">
                                            {name}
                                        </h1>
                                        {registration.status === "registered" ? (
                                            <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                                Registered
                                            </Badge>
                                        ) : (
                                            <Badge variant="outline" className="text-muted-foreground border-muted-foreground/30 bg-muted">
                                                Cancelled
                                            </Badge>
                                        )}
                                        {registration.hasReconfirmed && (
                                            <Badge variant="outline" className="text-primary border-primary/30 bg-primary/10">
                                                <CheckCircle className="size-3 mr-1" />
                                                Reconfirmed
                                            </Badge>
                                        )}
                                    </div>
                                    <div className="flex flex-wrap gap-x-5 gap-y-1 text-[13px] text-muted-foreground">
                                        <a
                                            href={`mailto:${registration.email}`}
                                            className="flex items-center gap-1.5 hover:text-foreground"
                                        >
                                            <Mail className="size-3.5" />
                                            {registration.email}
                                        </a>
                                        <span className="flex items-center gap-1.5">
                                            Registered {formatTs(registration.registeredAt)}
                                        </span>
                                    </div>
                                </div>
                            </div>
                            <div className="flex items-center gap-2 flex-none">
                                {registration.status === "registered" && (
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        className="text-destructive border-destructive/35 hover:bg-destructive/10 hover:text-destructive"
                                        onClick={() => {
                                            setCancelReason("");
                                            setCancelDialogOpen(true);
                                        }}
                                    >
                                        <Trash2 className="size-3.5" />
                                        Cancel registration
                                    </Button>
                                )}
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() =>
                                        toast.info("Coming soon", {
                                            description: "Changing ticket types is not yet available.",
                                        })
                                    }
                                >
                                    Change ticket types
                                </Button>
                            </div>
                        </div>
                    </Card>

                    {/* Two-column body */}
                    <div className="grid grid-cols-12 gap-5">
                        {/* Left column */}
                        <div className="col-span-12 lg:col-span-5 flex flex-col gap-5">
                            {/* Attendee details card */}
                            <Card className="p-5">
                                <div className="mb-3">
                                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                                        Attendee
                                    </div>
                                    <h3 className="font-display text-[18px] font-semibold mt-0.5">
                                        Details
                                    </h3>
                                </div>
                                <dl className="divide-y">
                                    {[
                                        ["Full name", name],
                                        [
                                            "Email",
                                            <a
                                                key="email"
                                                href={`mailto:${registration.email}`}
                                                className="text-primary hover:underline truncate inline-block max-w-full"
                                            >
                                                {registration.email}
                                            </a>,
                                        ],
                                        [
                                            "Registration ID",
                                            <span key="id" className="font-mono text-[12.5px]">
                                                {registration.id}
                                            </span>,
                                        ],
                                        ["Status", registration.status === "registered" ? "Registered" : "Cancelled"],
                                        [
                                            "Reconfirmed",
                                            registration.hasReconfirmed
                                                ? formatTs(registration.reconfirmedAt!)
                                                : "—",
                                        ],
                                        ...(Object.keys(registration.additionalDetails ?? {}).length > 0
                                            ? Object.entries(registration.additionalDetails).map(([k, v]) => [k, v])
                                            : []),
                                    ].map(([label, value], i) => (
                                        <div
                                            key={i}
                                            className="grid grid-cols-[130px_1fr] gap-3 py-2.5 text-[13.5px]"
                                        >
                                            <dt className="text-muted-foreground">{label}</dt>
                                            <dd className="min-w-0 truncate">{value}</dd>
                                        </div>
                                    ))}
                                </dl>
                            </Card>

                            {/* Tickets card */}
                            <Card className="p-5">
                                <div className="flex items-center justify-between mb-3">
                                    <div>
                                        <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                                            Tickets
                                        </div>
                                        <h3 className="font-display text-[18px] font-semibold mt-0.5">
                                            Selected
                                        </h3>
                                    </div>
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        className="text-muted-foreground"
                                        onClick={() =>
                                            toast.info("Coming soon", {
                                                description: "Changing ticket types is not yet available.",
                                            })
                                        }
                                    >
                                        Change
                                    </Button>
                                </div>
                                <div className="flex flex-col gap-3">
                                    {registration.tickets.length === 0 ? (
                                        <p className="text-sm text-muted-foreground">No tickets.</p>
                                    ) : (
                                        registration.tickets.map((ticket) => {
                                            const isCancelled = registration.status === "cancelled";
                                            return (
                                                <Card
                                                    key={ticket.slug}
                                                    className={`ticket-card overflow-hidden py-3 ${isCancelled ? "opacity-60" : ""}`}
                                                >
                                                    <div className="px-5">
                                                        <div className="flex items-center gap-2 mb-1">
                                                            <h4 className="font-display text-lg font-semibold">{ticket.name}</h4>
                                                            {isCancelled ? (
                                                                <Badge variant="secondary">Released</Badge>
                                                            ) : (
                                                                <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                                                    Active
                                                                </Badge>
                                                            )}
                                                        </div>
                                                        <div className="text-[12.5px] text-muted-foreground font-mono">{ticket.slug}</div>
                                                        <div className="ticket-perf" aria-hidden="true" />
                                                    </div>
                                                </Card>
                                            );
                                        })
                                    )}
                                </div>
                            </Card>
                        </div>

                        {/* Right column — Activity & emails */}
                        <div className="col-span-12 lg:col-span-7">
                            <Card className="p-5">
                                <div className="flex items-center justify-between mb-4">
                                    <div>
                                        <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                                            Timeline
                                        </div>
                                        <h3 className="font-display text-[18px] font-semibold mt-0.5">
                                            Activity &amp; emails
                                        </h3>
                                    </div>
                                    <div className="flex rounded-md border overflow-hidden text-[12px]">
                                        {(["all", "events", "emails"] as const).map((tab) => (
                                            <button
                                                key={tab}
                                                type="button"
                                                onClick={() => setTimelineFilter(tab)}
                                                className={`px-3 py-1.5 capitalize transition-colors ${
                                                    timelineFilter === tab
                                                        ? "bg-muted text-foreground font-medium"
                                                        : "text-muted-foreground hover:text-foreground"
                                                }`}
                                            >
                                                {tab === "all" ? "All" : tab === "events" ? "Events" : "Emails"}
                                            </button>
                                        ))}
                                    </div>
                                </div>

                                <ol className="relative">
                                    <div className="absolute left-[15px] top-1 bottom-1 w-px bg-border" />
                                    {visibleTimeline.length === 0 ? (
                                        <li className="text-[13px] text-muted-foreground pl-11 py-4">
                                            Nothing here yet.
                                        </li>
                                    ) : (
                                        visibleTimeline.map((entry, i) => (
                                            <TimelineItem key={i} entry={entry} />
                                        ))
                                    )}
                                </ol>
                            </Card>
                        </div>
                    </div>
                </>
            )}

            {/* Cancel dialog */}
            <AlertDialog
                open={cancelDialogOpen}
                onOpenChange={(open) => {
                    if (!open) {
                        setCancelDialogOpen(false);
                        setCancelReason("");
                    }
                }}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Cancel registration</AlertDialogTitle>
                        <AlertDialogDescription>
                            This will cancel the registration for{" "}
                            <strong>{name}</strong>. Please select a cancellation reason.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <Select value={cancelReason} onValueChange={setCancelReason}>
                        <SelectTrigger>
                            <SelectValue placeholder="Select a reason…" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="AttendeeRequest">Attendee request</SelectItem>
                            <SelectItem value="VisaLetterDenied">Visa letter denied</SelectItem>
                        </SelectContent>
                    </Select>
                    <AlertDialogFooter>
                        <AlertDialogCancel disabled={isCancelling}>Cancel</AlertDialogCancel>
                        <Button
                            variant="destructive"
                            disabled={!cancelReason || isCancelling}
                            onClick={handleCancelConfirm}
                        >
                            {isCancelling ? "Cancelling…" : "Confirm cancellation"}
                        </Button>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </PageLayout>
    );
}

// ── Timeline item component ───────────────────────────────────────────────────

const kindMeta: Record<
    TimelineKind,
    { color: string; bgClass: string; borderClass: string; Icon: React.ElementType }
> = {
    registered: {
        color: "text-primary",
        bgClass: "bg-primary/6",
        borderClass: "border-primary/25",
        Icon: Sparkles,
    },
    reconfirmed: {
        color: "text-success",
        bgClass: "bg-success/6",
        borderClass: "border-success/25",
        Icon: CheckCircle,
    },
    cancelled: {
        color: "text-destructive",
        bgClass: "bg-destructive/6",
        borderClass: "border-destructive/25",
        Icon: Trash2,
    },
    email: {
        color: "text-muted-foreground",
        bgClass: "bg-muted/60",
        borderClass: "border-border",
        Icon: Mail,
    },
};

function TimelineItem({ entry }: { entry: TimelineEntry }) {
    const meta = kindMeta[entry.kind] ?? kindMeta.registered;
    const { Icon } = meta;

    return (
        <li className="relative pl-11 pb-5 last:pb-0">
            <span
                className={`absolute left-0 top-0 h-8 w-8 rounded-full grid place-items-center border ${meta.color} ${meta.bgClass} ${meta.borderClass}`}
            >
                <Icon className="size-3.5" />
            </span>
            <div className="flex items-baseline justify-between gap-3">
                <div className="min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-[14px] font-medium">{entry.title}</span>
                        <Badge variant="outline" className="text-[0.65rem] text-muted-foreground capitalize">
                            {entry.kind}
                        </Badge>
                    </div>
                    <div className="text-[12.5px] text-muted-foreground mt-0.5">{entry.detail}</div>
                    {entry.kind === "email" && (
                        <div className="mt-2 flex items-center gap-2">
                            <Button
                                variant="ghost"
                                size="sm"
                                className="h-7 text-[12px]"
                                onClick={() =>
                                    toast.info("View email", { description: "This feature is coming soon." })
                                }
                            >
                                <Eye className="size-3" /> View
                            </Button>
                            <Button
                                variant="ghost"
                                size="sm"
                                className="h-7 text-[12px]"
                                onClick={() =>
                                    toast.info("Resend email", { description: "This feature is coming soon." })
                                }
                            >
                                <RotateCcw className="size-3" /> Resend
                            </Button>
                        </div>
                    )}
                </div>
                <span className="text-[11.5px] text-muted-foreground whitespace-nowrap flex-none font-mono tabular-nums">
                    {formatRelative(entry.ts)}
                </span>
            </div>
        </li>
    );
}
