"use client";

import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import * as z from "zod";
import { useFieldArray } from "react-hook-form";
import { Plus, Trash2 } from "lucide-react";
import { useCustomForm } from "@/hooks/use-custom-form";
import { FormHeading } from "@/components/form-heading";

const eventInfo = {
    name: "Azure Fest 2025",
    date: new Date("2023-10-01"),
    ticketTypes: [
        {
            id: "1",
            name: "General Admission"
        },
        {
            id: "2",
            name: "VIP Admission"
        }
    ]
}

const ticketSchema = z.object({
    id: z.string()
});

const formSchema = z.object({
    firstName: z.string(),//.min(1),
    lastName: z.string(),
    companyName: z.string(),
    tickets: z.array(ticketSchema).optional()
});

export function AddRegistrationForm()
{
    const form = useCustomForm<z.infer<typeof formSchema>>(
        formSchema,
        {
            firstName: "",
            lastName: "",
            companyName: "",
            tickets: []
        });

    const { fields, append, remove } = useFieldArray({
        control: form.control,
        name: "tickets"
    });

    async function onSubmit(values: z.infer<typeof formSchema>)
    {
        console.log(values);
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-8">

                <FormHeading text="Event" />

                <FormItem>
                    <FormLabel>Event name</FormLabel>
                    <Input disabled={true} value={eventInfo.name} />
                </FormItem>

                <FormHeading text="Attendee details" />

                <FormField
                    control={form.control}
                    name="firstName"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>First name</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. Jane" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="lastName"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Last name</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. Doe" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="companyName"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Company</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. Acme Inc." {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormHeading text="Tickets" />

                {/* Tickets Section */}
                <FormLabel className="my-2">Tickets</FormLabel>
                <div className="border p-4 rounded-lg space-y-4 bg-muted/20">
                    {fields.map((field, index) => (

                        <div
                            key={field.id}
                            className="border p-4 rounded-md bg-white shadow-sm"
                        >
                            <div className="grid grid-cols-[1fr_1fr_1fr_auto] gap-4 items-start">

                                {/* Labels Row */}
                                <div className="flex flex-col">
                                    <FormLabel className="text-sm font-medium text-gray-700">Ticket name</FormLabel>
                                </div>

                                {/* Controls Row */}
                                <FormField
                                    control={form.control}
                                    name={`tickets.${index}.id`}
                                    render={({ field }) => (
                                        <FormItem className="flex flex-col">
                                            <FormControl>
                                                <select
                                                    {...field}
                                                    className="block w-full rounded-md border border-input bg-white px-3 py-2 text-sm shadow-sm focus:ring-2 focus:ring-ring focus:ring-offset-2"
                                                >
                                                    <option value="" disabled>
                                                        Select a ticket type
                                                    </option>
                                                    {eventInfo.ticketTypes.map((ticket) => (
                                                        <option key={ticket.id} value={ticket.id}>
                                                            {ticket.name}
                                                        </option>
                                                    ))}
                                                </select>
                                            </FormControl>
                                            <FormMessage className="text-xs mt-1" />
                                        </FormItem>
                                    )}
                                />


                                <div className="flex items-center justify-between gap-2">
                                    <Button
                                        type="button"
                                        variant="ghost"
                                        size="icon"
                                        className="shrink-0"
                                        onClick={() => remove(index)}
                                    >
                                        <Trash2 className="h-5 w-5" />
                                    </Button>
                                </div>
                            </div>
                        </div>

                    ))}

                    <Button
                        type="button"
                        onClick={() => append({ id: "" })}
                        variant="secondary"
                    >
                        <Plus />Add ticket type
                    </Button>
                </div>

                <Button type="submit">Add registration</Button>
            </form>
        </Form>
    );
}
