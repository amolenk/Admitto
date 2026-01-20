"use client";

import {AttendeeDto, CheckInResponse} from "@/lib/admitto-api/generated";
import { ColumnDef } from "@tanstack/react-table";
import { MoreHorizontal } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger
} from "@/components/ui/dropdown-menu";
import { DataTableColumnHeader } from "@/(dashboard)/teams/[teamSlug]/events/[eventSlug]/data-table-column-header";

export const createColumns = (teamSlug: string, eventSlug: string): ColumnDef<AttendeeDto>[] => {

    const checkInAttendee = async (attendeeId: string) => {
        const endpoint = `/api/teams/${encodeURIComponent(teamSlug)}/events/${encodeURIComponent(
            eventSlug
        )}/attendees/${encodeURIComponent(attendeeId)}/checkin`;

        const res = await fetch(endpoint, {
            method: "POST",
            headers: {"Content-Type": "application/json"},
            cache: "no-store",
        });

        if (!res.ok) {
            const msg = await res.text().catch(() => "");
            alert(msg || `Server returned ${res.status}`);
            return;
        }

        const data = (await res.json()) as CheckInResponse;

        alert(`Checked in attendee ${data.firstName} ${data.lastName} (${data.attendeeStatus})`);
    };

    return [
        {
            accessorFn: (row) => `${row.firstName} ${row.lastName}`,
            id: "name",  // Required when using accessorFn
            header: ({column}) => (
                <DataTableColumnHeader column={column} title="Name"/>
            ),
            cell: ({row}) => {
                return <div className="px-3">{row.getValue<string>("name")}</div>;
            },
            filterFn: (row, columnId, filterValue) => {
                const name = row.getValue<string>("name")?.toLowerCase() || "";
                const email = row.getValue<string>("email")?.toLowerCase() || "";
                const query = filterValue.toLowerCase();
                return name.includes(query) || email.includes(query);
            },
        },
        {
            accessorKey: "email",
            header: ({column}) => (
                <DataTableColumnHeader column={column} title="Email"/>
            ),
            cell: ({row}) => {
                return <div className="px-3">{row.getValue("email")}</div>;
            }
        },
        {
            accessorFn: (row) => row.tickets.map(t => t.ticketTypeSlug),
            id: "tickets",
            header: ({column}) => (
                <DataTableColumnHeader column={column} title="Tickets"/>
            ),
            cell: ({row}) => {
                const ticketSlugs = row.getValue<string[]>("tickets");
                return <div className="px-3">{ticketSlugs.sort().join(", ")}</div>;
            },
            filterFn: "arrIncludesSome"
        },
        {
            accessorKey: "status",
            header: ({column}) => (
                <DataTableColumnHeader column={column} title="Status"/>
            ),
            cell: ({row}) => {

                return <div className="px-3">{row.getValue("status")}</div>;
            },
            filterFn: "arrIncludesSome"
        },
        {
            id: "actions",
            cell: ({row}) => {
                const attendee = row.original;

                return (
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="h-8 w-8 p-0">
                                <span className="sr-only">Open menu</span>
                                <MoreHorizontal className="h-4 w-4"/>
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                            <DropdownMenuLabel>Actions</DropdownMenuLabel>
                            <DropdownMenuItem
                                onClick={() => navigator.clipboard.writeText(attendee.email)}
                            >
                                Copy email
                            </DropdownMenuItem>
                            <DropdownMenuSeparator/>
                            <DropdownMenuItem
                                onClick={() => checkInAttendee(attendee.attendeeId)}>
                                Check in
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
                );
            }
        }
    ];
}

