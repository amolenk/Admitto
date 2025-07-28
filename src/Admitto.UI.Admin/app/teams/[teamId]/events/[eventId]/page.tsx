"use client";

import * as React from "react";
import { useCallback } from "react";
import { useDataLoader } from "@/hooks/use-data-loader";
import { useParams } from "next/navigation";
import { PageLayout } from "@/components/page-layout";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { attendees } from "@/teams/[teamId]/events/[eventId]/attendee";
import { columns } from "@/teams/[teamId]/events/[eventId]/columns";
import { DataTable } from "@/teams/[teamId]/events/[eventId]/data-table";

export default function ViewEventPage()
{
    const { teamId, eventId } = useParams();

    const loadData = useCallback(async () =>
    {
        // const [eventResponse, teamResponse, attendeesResponse] = await Promise.all([
        //     fetch(`/api/teams/${teamId}/events/${eventId}`),
        //     fetch(`/api/teams/${teamId}`),
        //     fetch(`/api/teams/${teamId}/events/${eventId}/attendees`)
        // ]);
        //
        // if (!eventResponse.ok || !teamResponse.ok || !attendeesResponse.ok)
        // {
        //     throw new Error("Failed to fetch data from one or more endpoints");
        // }
        //
        // const [eventData, teamData, attendeesData] = await Promise.all([
        //     eventResponse.json(),
        //     teamResponse.json(),
        //     attendeesResponse.json()
        // ]);
        //
        // return { eventData, teamData, attendeesData };

        const response = await fetch(`/api/teams/${teamId}/events/${eventId}`);
        if (!response.ok)
        {
            throw new Error("Failed to fetch event data");
        }
        return response.json();
    }, [teamId, eventId]);

    const { data: eventData, loading, error } = useDataLoader(loadData);

    if (loading)
    {
        return <div>Loading...</div>;
    }

    if (error)
    {
        return <div>{error}</div>;
    }

    return (
        <PageLayout title={eventData.eventName}>
            <div className="flex flex-col gap-4 py-4 md:gap-6 md:py-6">

                <Tabs defaultValue="attendees">
                    <TabsList>
                        {/*<TabsTrigger value="overview">Overview</TabsTrigger>*/}
                        <TabsTrigger value="attendees">Attendees</TabsTrigger>
                    </TabsList>
                    {/*<TabsContent*/}
                    {/*    value="overview"*/}
                    {/*    className="relative flex flex-col gap-4 overflow-auto px-4 lg:px-6"*/}
                    {/*>*/}
                    {/*    <SectionCards />*/}

                    {/*    <div className="px-4 lg:px-6">*/}
                    {/*        <ChartAreaInteractive />*/}
                    {/*    </div>*/}
                    {/*</TabsContent>*/}
                    <TabsContent value="attendees">
                        <DataTable teamId={`${teamId}`} eventId={`${eventId}`} columns={columns} data={attendees} />
                    </TabsContent>
                </Tabs>
            </div>
        </PageLayout>
    );
}
