"use client";

import { Button } from "@/components/ui/button";
import { DateRangePicker } from "@/components/ui/date-range-picker";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import * as z from "zod";
import { useFieldArray } from "react-hook-form";
import { Plus, Trash2 } from "lucide-react";
import { useCustomForm } from "@/hooks/use-custom-form";
import { FormHeading } from "@/components/form-heading";
import { Switch } from "@/components/ui/switch";
import { FormTooltipLabel } from "@/components/form-tooltip-label";

const ticketTypeSchema = z.object({
    name: z.string(),//.min(1),
    group: z.string(),
    maxCapacity: z.number().int(),//.min(1),
    private: z.boolean()
});

const formSchema = z.object({
    name: z.string(),//.min(1),
    eventDates: z.object(
        {
            from: z.date().optional(),
            to: z.date().optional()
        },
        // {
        //     required_error: "Please select a date range"
        // }
    ),
    registrationPeriod: z.object(
        {
            from: z.date().optional(),
            to: z.date().optional(),
        },
        // {
        //     required_error: "Please select a date range"
        // }
    ),
    ticketTypes: z.array(ticketTypeSchema).optional()
});
// .refine((data) => data.dateRange.from < data.dateRange.to, {
//     path: ["dateRange"],
//     message: "From date must be before to date",
// });

export function AddEventForm()
{
    const form = useCustomForm<z.infer<typeof formSchema>>(
        formSchema,
        {
            name: "",
            eventDates: {},
            registrationPeriod: {},
            ticketTypes: [
                {
                    name: "",
                    group: "",
                    maxCapacity: 100,
                    private: false
                }
            ]
        });

    const { fields, append, remove } = useFieldArray({
        control: form.control,
        name: "ticketTypes"
    });

    async function onSubmit(values: z.infer<typeof formSchema>)
    {
        console.log(values);
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-8">

                <FormHeading text="Basic info" />

                <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Event name</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. Technopoloza" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="eventDates"
                    render={({ field }) => (
                        <DateRangePicker field={field} label="Event date(s)" />
                    )}
                />

                <FormHeading text="Tickets" />

                <FormField
                    control={form.control}
                    name="registrationPeriod"
                    render={({ field }) => (
                        <DateRangePicker field={field} label="Registration period" />
                    )}
                />

                {/* Ticket Types Section */}
                <FormLabel className="my-2">Ticket types</FormLabel>
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
                                <div className="flex flex-col">
                                    <FormTooltipLabel labelText="Group"
                                                      helpText="Tickets in the same group are mutually exclusive" />
                                </div>
                                <div className="flex flex-col">
                                    <FormLabel className="text-sm font-medium text-gray-700">Maximum
                                        capacity</FormLabel>
                                </div>
                                <div className="flex flex-col pr-1">
                                    <FormTooltipLabel labelText="Private"
                                                      helpText="Private ticket types can only be managed through the admin UI" />
                                </div>

                                {/* Controls Row */}
                                <FormField
                                    control={form.control}
                                    name={`ticketTypes.${index}.name`}
                                    render={({ field }) => (
                                        <FormItem className="flex flex-col">
                                            <FormControl>
                                                <Input placeholder="e.g. General Admission" {...field} />
                                            </FormControl>
                                            <FormMessage className="text-xs mt-1" />
                                        </FormItem>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name={`ticketTypes.${index}.group`}
                                    render={({ field }) => (
                                        <FormItem className="flex flex-col">
                                            <FormControl>
                                                <Input placeholder="e.g. Morning Workshop" {...field} />
                                            </FormControl>
                                            <FormMessage className="text-xs mt-1" />
                                        </FormItem>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name={`ticketTypes.${index}.maxCapacity`}
                                    render={({ field }) => (
                                        <FormItem className="flex flex-col">
                                            <FormControl>
                                                <Input
                                                    type="number"
                                                    min={1}
                                                    {...field}
                                                    onChange={(e) => field.onChange(e.target.valueAsNumber)}
                                                />
                                            </FormControl>
                                            <FormMessage className="text-xs mt-1" />
                                        </FormItem>
                                    )}
                                />

                                <div className="flex items-center justify-between gap-2">
                                    <FormField
                                        control={form.control}
                                        name={`ticketTypes.${index}.private`}
                                        render={({ field }) => (
                                            <FormItem className="flex items-center gap-2">
                                                <FormControl>
                                                    <Switch
                                                        checked={field.value}
                                                        onCheckedChange={(e) => field.onChange(e.valueOf())}
                                                    />
                                                </FormControl>
                                            </FormItem>
                                        )}
                                    />

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
                        onClick={() => append({ name: "", group: "", maxCapacity: 100, private: false })}
                        variant="secondary"
                    >
                        <Plus />Add ticket type
                    </Button>
                </div>

                <Button type="submit">Create event</Button>
            </form>
        </Form>
    );
}
