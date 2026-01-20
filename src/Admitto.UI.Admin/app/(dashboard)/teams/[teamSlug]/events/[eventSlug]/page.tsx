"use client";

import {createColumns} from "@/(dashboard)/teams/[teamSlug]/events/[eventSlug]/columns";
import {DataTable} from "@/(dashboard)/teams/[teamSlug]/events/[eventSlug]/data-table";
import {QrCodeScanner} from "@/components/qrcode-scanner";
import {AttendeeDto} from "@/lib/admitto-api/generated";
import * as React from "react";
import { useCallback } from "react";
import { useDataLoader } from "@/hooks/use-data-loader";
import { useParams } from "next/navigation";
import { PageLayout } from "@/components/page-layout";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export default function ViewEventPage()
{
    const { teamSlug, eventSlug } = useParams();

    const loadData = useCallback(async () =>
    {
        const [/*eventResponse, teamResponse,*/ attendeesResponse] = await Promise.all([
            // fetch(`/api/teams/${teamId}/events/${eventId}`),
            // fetch(`/api/teams/${teamId}`),
            fetch(`/api/teams/${teamSlug}/events/${eventSlug}/attendees`)
        ]);

        if (/*!eventResponse.ok || !teamResponse.ok ||*/ !attendeesResponse.ok)
        {
            throw new Error("Failed to fetch data from one or more endpoints");
        }

        const attendeesData = (await attendeesResponse.json()) as AttendeeDto[] | undefined;
        return attendeesData ?? [];

    }, [teamSlug, eventSlug]);

    const { data: attendees, loading, error } = useDataLoader(loadData);

    if (loading)
    {
        return <div>Loading...</div>;
    }

    if (error)
    {
        return <div>{error} - Team slug = "{teamSlug}"</div>;
    }

    return (
        <PageLayout title="My Event">
            <div className="flex flex-col gap-4 py-4 md:gap-6 md:py-6">

                <Tabs defaultValue="attendees">
                    <TabsList>
                        <TabsTrigger value="attendees">Attendees</TabsTrigger>
                        <TabsTrigger value="overview">Ticket Scanner</TabsTrigger>
                    </TabsList>
                    <TabsContent value="attendees">
                        <DataTable columns={createColumns(teamSlug as string, eventSlug as string)} data={attendees ?? []} />
                    </TabsContent>
                    <TabsContent
                        value="overview"
                        className="relative flex flex-col gap-4 overflow-auto px-4 lg:px-6"
                    >
                        <QrCodeScanner teamSlug={teamSlug as string} eventSlug={eventSlug as string} />
                    </TabsContent>
                </Tabs>
            </div>
        </PageLayout>
    );
}
