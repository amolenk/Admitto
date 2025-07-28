"use client";

import * as React from "react";
import {
    ColumnDef,
    ColumnFiltersState,
    flexRender,
    getCoreRowModel,
    getFacetedRowModel,
    getFacetedUniqueValues,
    getFilteredRowModel,
    getPaginationRowModel,
    getSortedRowModel,
    SortingState,
    useReactTable,
    VisibilityState
} from "@tanstack/react-table";

import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

import { DataTablePagination } from "./data-table-pagination";
import { DataTableToolbar } from "./data-table-toolbar";

interface DataTableProps<TData, TValue>
{
    columns: ColumnDef<TData, TValue>[];
    data: TData[];
}

export function DataTable<TData, TValue>({
                                             columns,
                                             data
                                         }: DataTableProps<TData, TValue>)
{
    const [rowSelection, setRowSelection] = React.useState({});
    const [columnVisibility, setColumnVisibility] =
        React.useState<VisibilityState>({});
    const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
        []
    );
    const [sorting, setSorting] = React.useState<SortingState>([]);

    const table = useReactTable({
        data,
        columns,
        state: {
            sorting,
            columnVisibility,
            rowSelection,
            columnFilters
        },
        enableRowSelection: true,
        onRowSelectionChange: setRowSelection,
        onSortingChange: setSorting,
        onColumnFiltersChange: setColumnFilters,
        onColumnVisibilityChange: setColumnVisibility,
        getCoreRowModel: getCoreRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getFacetedRowModel: getFacetedRowModel(),
        getFacetedUniqueValues: getFacetedUniqueValues()
    });

    return (
        <div className="space-y-4">
            <DataTableToolbar table={table} />
            <div className="rounded-md border">
                <Table>
                    <TableHeader>
                        {table.getHeaderGroups().map((headerGroup) => (
                            <TableRow key={headerGroup.id}>
                                {headerGroup.headers.map((header) =>
                                {
                                    return (
                                        <TableHead key={header.id} colSpan={header.colSpan}>
                                            {header.isPlaceholder
                                                ? null
                                                : flexRender(
                                                    header.column.columnDef.header,
                                                    header.getContext()
                                                )}
                                        </TableHead>
                                    );
                                })}
                            </TableRow>
                        ))}
                    </TableHeader>
                    <TableBody>
                        {table.getRowModel().rows?.length ? (
                            table.getRowModel().rows.map((row) => (
                                <TableRow
                                    key={row.id}
                                    data-state={row.getIsSelected() && "selected"}
                                >
                                    {row.getVisibleCells().map((cell) => (
                                        <TableCell key={cell.id}>
                                            {flexRender(
                                                cell.column.columnDef.cell,
                                                cell.getContext()
                                            )}
                                        </TableCell>
                                    ))}
                                </TableRow>
                            ))
                        ) : (
                            <TableRow>
                                <TableCell
                                    colSpan={columns.length}
                                    className="h-24 text-center"
                                >
                                    No results.
                                </TableCell>
                            </TableRow>
                        )}
                    </TableBody>
                </Table>
            </div>
            <DataTablePagination table={table} />
        </div>
    );
}

// "use client";
//
// import * as React from "react";
// import {
//     ColumnDef,
//     ColumnFiltersState,
//     flexRender,
//     getCoreRowModel,
//     getFilteredRowModel,
//     getPaginationRowModel,
//     getSortedRowModel,
//     SortingState,
//     useReactTable
// } from "@tanstack/react-table";
// import { Button } from "@/components/ui/button";
// import { Input } from "@/components/ui/input";
// import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
// import { attendees } from "@/teams/[teamId]/events/[eventId]/attendee";
// import MultipleSelector, { Option } from "@/components/ui/multiple-selector";
// import { MoreHorizontal } from "lucide-react";
// import { useRouter } from "next/navigation";
// import {
//     DropdownMenu,
//     DropdownMenuContent,
//     DropdownMenuItem,
//     DropdownMenuTrigger
// } from "@/components/ui/dropdown-menu";
// import { DataTablePagination } from "@/teams/[teamId]/events/[eventId]/data-table-pagination";
//
//
// interface DataTableProps<TData, TValue>
// {
//     teamId: string;
//     eventId: string;
//     columns: ColumnDef<TData, TValue>[];
//     data: TData[];
// }
//
// export function DataTable<TData, TValue>({
//                                              teamId,
//                                              eventId,
//                                              columns,
//                                              data
//                                          }: DataTableProps<TData, TValue>)
// {
//     const router = useRouter();
//     const [sorting, setSorting] = React.useState<SortingState>([]);
//     const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
//         []
//     );
//
//     const table = useReactTable({
//         data,
//         columns,
//         getCoreRowModel: getCoreRowModel(),
//         getPaginationRowModel: getPaginationRowModel(),
//         onSortingChange: setSorting,
//         getSortedRowModel: getSortedRowModel(),
//         onColumnFiltersChange: setColumnFilters,
//         getFilteredRowModel: getFilteredRowModel(),
//         state: {
//             sorting,
//             columnFilters
//         }
//     });
//
//     const ticketTypeOptions = attendees
//         .flatMap((a) => a.ticketTypes) // Flatten the ticket types
//         .filter((type, index, self) => self.indexOf(type) === index) // Remove duplicates
//         .sort() // Sort alphabetically
//         .map((ticketType) => ({ label: ticketType, value: ticketType, key: ticketType })); // Map to Option objects
//
//     const statusOptions: Option[] = [
//         { label: "Attended", value: "attended" },
//         { label: "Confirmed", value: "confirmed" },
//         { label: "No-show", value: "no-show" },
//         { label: "Processing", value: "processing" },
//         { label: "Registered", value: "registered" }
//     ];
//
//     return (
//         <div>
//             <div className="flex gap-2">
//                 <div className="flex flex-grow">
//                     <Input
//                         placeholder="Search by name..."
//                         value={(table.getColumn("name")?.getFilterValue() as string) ?? ""}
//                         onChange={(event) =>
//                             table.getColumn("name")?.setFilterValue(event.target.value)
//                         }
//                         // className="max-w-sm"
//                     />
//                 </div>
//                 <div className="flex flex-grow">
//                     <Input
//                         placeholder="Search by e-mail..."
//                         value={(table.getColumn("email")?.getFilterValue() as string) ?? ""}
//                         onChange={(event) =>
//                             table.getColumn("email")?.setFilterValue(event.target.value)
//                         }
//                         // className="max-w-sm"
//                     />
//                 </div>
//                 <div className="flex items-right">
//                     <DropdownMenu>
//                         <DropdownMenuTrigger asChild>
//                             <Button variant="outline">
//                                 <MoreHorizontal className="h-4 w-4" />
//                                 <span className="sr-only">Actions</span>
//                             </Button>
//                             {/*<Button variant="ghost" className="h-8 w-8 p-0">*/}
//                             {/*    <span className="sr-only">Open menu</span>*/}
//                             {/*    <MoreHorizontal className="h-4 w-4" />*/}
//                             {/*</Button>*/}
//                         </DropdownMenuTrigger>
//                         <DropdownMenuContent align="end">
//                             <DropdownMenuItem
//                                 onClick={() => router.push(`/teams/${teamId}/events/${eventId}/registrations/add`)}
//                             >
//                                 <span className="hidden lg:inline">Add registration</span>
//                             </DropdownMenuItem>
//                             <DropdownMenuItem
//                                 onClick={() => router.push(`/teams/${teamId}/events/${eventId}/registrations/add`)}
//                             >
//                                 <span className="hidden lg:inline">Request confirmations</span>
//                             </DropdownMenuItem>
//                         </DropdownMenuContent>
//                     </DropdownMenu>
//                 </div>
//             </div>
//             <div className="flex py-4 gap-2">
//                 <div className="flex flex-grow">
//                     <MultipleSelector
//                         defaultOptions={ticketTypeOptions}
//                         hidePlaceholderWhenSelected={true}
//                         placeholder="Filter by ticket types..."
//                         emptyIndicator={
//                             <p className="text-center text-lg leading-10 text-gray-400 dark:text-gray-400">
//                                 No other ticket types found.
//                             </p>
//                         }
//                         onChange={(event) =>
//                         {
//                             const values = event.map(o => o.value);
//                             table.getColumn("ticketTypes")?.setFilterValue(values);
//                         }}
//                     />
//                 </div>
//                 <div className="flex flex-grow">
//                     <MultipleSelector
//                         defaultOptions={statusOptions}
//                         hidePlaceholderWhenSelected={true}
//                         placeholder="Filter by status..."
//                         emptyIndicator={
//                             <p className="text-center text-lg leading-10 text-gray-400 dark:text-gray-400">
//                                 No other statuses found.
//                             </p>
//                         }
//                         onChange={(event) =>
//                         {
//                             const values = event.map(o => o.value);
//                             table.getColumn("status")?.setFilterValue(values);
//                         }}
//                     />
//                 </div>
//             </div>
//             <div className="rounded-md border">
//                 <Table>
//                     <TableHeader>
//                         {table.getHeaderGroups().map((headerGroup) => (
//                             <TableRow key={headerGroup.id}>
//                                 {headerGroup.headers.map((header) =>
//                                 {
//                                     return (
//                                         <TableHead key={header.id}>
//                                             {header.isPlaceholder
//                                                 ? null
//                                                 : flexRender(
//                                                     header.column.columnDef.header,
//                                                     header.getContext()
//                                                 )}
//                                         </TableHead>
//                                     );
//                                 })}
//                             </TableRow>
//                         ))}
//                     </TableHeader>
//                     <TableBody>
//                         {table.getRowModel().rows?.length ? (
//                             table.getRowModel().rows.map((row) => (
//                                 <TableRow
//                                     key={row.id}
//                                     data-state={row.getIsSelected() && "selected"}
//                                 >
//                                     {row.getVisibleCells().map((cell) => (
//                                         <TableCell key={cell.id}>
//                                             {flexRender(cell.column.columnDef.cell, cell.getContext())}
//                                         </TableCell>
//                                     ))}
//                                 </TableRow>
//                             ))
//                         ) : (
//                             <TableRow>
//                                 <TableCell colSpan={columns.length} className="h-24 text-center">
//                                     No results.
//                                 </TableCell>
//                             </TableRow>
//                         )}
//                     </TableBody>
//                 </Table>
//             </div>
//             <div className="flex items-center justify-end space-x-2 py-4">
//                 <DataTablePagination table={table} />
//
//                 {/*<Button*/}
//                 {/*    variant="outline"*/}
//                 {/*    size="sm"*/}
//                 {/*    onClick={() => table.previousPage()}*/}
//                 {/*    disabled={!table.getCanPreviousPage()}*/}
//                 {/*>*/}
//                 {/*    Previous*/}
//                 {/*</Button>*/}
//                 {/*<Button*/}
//                 {/*    variant="outline"*/}
//                 {/*    size="sm"*/}
//                 {/*    onClick={() => table.nextPage()}*/}
//                 {/*    disabled={!table.getCanNextPage()}*/}
//                 {/*>*/}
//                 {/*    Next*/}
//                 {/*</Button>*/}
//             </div>
//         </div>
//     );
// }
