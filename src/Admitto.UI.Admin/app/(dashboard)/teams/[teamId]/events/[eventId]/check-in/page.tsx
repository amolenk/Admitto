"use client";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { CheckInSummaryDto, TicketedEventDetailsDto } from "@/lib/admitto-api/generated/types.gen";
import { CheckInScanner } from "./scanner";
export default function CheckInPage() { const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>(); const query = useQuery({ queryKey: ["event", teamId, eventId], queryFn: () => apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`) }); const summary = useQuery({ queryKey: ["check-in-summary", teamId, eventId], queryFn: () => apiClient.get<CheckInSummaryDto>(`/api/teams/${teamId}/events/${eventId}/registrations/check-in/summary`), retry: false }); return <PageLayout><Button variant="ghost" asChild><Link href={`/teams/${teamId}/events/${eventId}`}><ArrowLeft /> Event dashboard</Link></Button>{query.isLoading ? <Skeleton className="mx-auto h-[600px] max-w-2xl" /> : query.data ? <CheckInScanner teamId={teamId} eventId={eventId} startsAt={query.data.startsAt} timeZone={query.data.timeZone} summary={summary.data} /> : <p className="text-destructive">Failed to load event.</p>}</PageLayout>; }
