"use client";

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
import { Attendee } from "@/teams/[teamId]/events/[eventId]/attendee";
import { DataTableColumnHeader } from "@/teams/[teamId]/events/[eventId]/data-table-column-header";

export const columns: ColumnDef<Attendee>[] = [
    {
        accessorKey: "name",
        header: ({ column }) => (
            <DataTableColumnHeader column={column} title="Name" />
        ),
        cell: ({ row }) =>
        {
            return <div className="px-3">{row.getValue("name")}</div>;
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
        header: ({ column }) => (
            <DataTableColumnHeader column={column} title="Email" />
        ),
        cell: ({ row }) =>
        {
            return <div className="px-3">{row.getValue("email")}</div>;
        }
    },
    {
        accessorKey: "ticketTypes",
        header: ({ column }) => (
            <DataTableColumnHeader column={column} title="Tickets" />
        ),
        cell: ({ row }) =>
        {

            const ticketTypes: string[] = row.getValue("ticketTypes");
            const ticketTypeNames = ticketTypes.sort().join(", ");
            return <div className="px-3">{ticketTypeNames}</div>;
        },
        filterFn: "arrIncludesAll"
    },
    {
        accessorKey: "status",
        header: ({ column }) => (
            <DataTableColumnHeader column={column} title="Status" />
        ),
        cell: ({ row }) =>
        {

            return <div className="px-3">{row.getValue("status")}</div>;
        }
    },
    {
        id: "actions",
        cell: ({ row }) =>
        {
            const attendee = row.original;

            return (
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <Button variant="ghost" className="h-8 w-8 p-0">
                            <span className="sr-only">Open menu</span>
                            <MoreHorizontal className="h-4 w-4" />
                        </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                        <DropdownMenuLabel>Actions</DropdownMenuLabel>
                        <DropdownMenuItem
                            onClick={() => navigator.clipboard.writeText(attendee.email)}
                        >
                            Copy email
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem>View attendee details</DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            );
        }
    }
];

