"use client";

import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { useTeamStore } from "@/stores/team-store";
import { useRouter } from "next/navigation";
import { CreateTeamResponse } from "@/api-client";
import { useCustomForm } from "@/hooks/use-custom-form";
import { FormAlert } from "@/components/form-alert";
import { FormError } from "@/components/form-error";
import { FormHeading } from "@/components/form-heading";
import { Plus, Trash2 } from "lucide-react";
import { useFieldArray } from "react-hook-form";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";


const memberSchema = z.object({
    email: z.string(),//.min(1),
    role: z.string()
});

const formSchema = z.object({
    name: z.string()
        .min(2, { message: "Team name must be at least 2 characters." })
        .max(30, { message: "Team name must not be longer than 30 characters." }),
    members: z.array(memberSchema).optional()
});

export function AddTeamForm()
{
    const router = useRouter();
    const { fetchTeams } = useTeamStore();

    const form = useCustomForm<z.infer<typeof formSchema>>(
        formSchema,
        {
            name: "",
            members: []
        });

    const { fields, append, remove } = useFieldArray({
        control: form.control,
        name: "members"
    });

    async function onSubmit(values: z.infer<typeof formSchema>)
    {
        console.log(values);

        // const response = await fetch("/api/teams", {
        //     method: "POST",
        //     headers: { "Content-Type": "application/json" },
        //     body: JSON.stringify({ name: values.name })
        // });
        //
        // if (!response.ok)
        // {
        //     throw new FormError(await response.json());
        // }
        //
        // const result: CreateTeamResponse = await response.json();
        // await fetchTeams(result.id);
        //
        // // Redirect to home page
        // router.push("/");
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-8">
                {form.generalError && <FormAlert error={form.generalError} />}

                <FormHeading text="Basic info" />

                <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Name</FormLabel>
                            <FormControl>
                                <Input placeholder="Toon Squad" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormHeading text="Members" />

                {/*<FormLabel className="my-2">Team members</FormLabel>*/}
                <div className="border p-4 rounded-lg space-y-4 bg-muted/20">
                    {fields.map((field, index) => (

                        <div
                            key={field.id}
                            className="border p-4 rounded-md bg-white shadow-sm"
                        >
                            <div className="grid grid-cols-[1fr_auto] gap-4 items-start">

                                {/* Labels Row */}
                                <div className="flex flex-col">
                                    <FormLabel className="text-sm font-medium text-gray-700">E-mail</FormLabel>
                                </div>
                                <div className="flex flex-col">
                                    <FormLabel className="text-sm font-medium text-gray-700">Role</FormLabel>
                                </div>

                                {/* Controls Row */}
                                <FormField
                                    control={form.control}
                                    name={`members.${index}.email`}
                                    render={({ field }) => (
                                        <FormItem className="flex flex-col">
                                            <FormControl>
                                                <Input placeholder="e.g. jdoe@example.com" {...field} />
                                            </FormControl>
                                            <FormMessage className="text-xs mt-1" />
                                        </FormItem>
                                    )}
                                />

                                <div className="flex items-center justify-between gap-2">

                                    <FormField
                                        control={form.control}
                                        name={`members.${index}.role`}
                                        render={({ field }) => (
                                            <FormItem className="flex flex-col">
                                                <FormControl>
                                                    <Select {...field}>
                                                        <SelectTrigger className="w-[180px]">
                                                            <SelectValue placeholder="Select a role" />
                                                        </SelectTrigger>
                                                        <SelectContent>
                                                            <SelectItem value="manager">Manager</SelectItem>
                                                            <SelectItem value="organizer">Organizer</SelectItem>
                                                        </SelectContent>
                                                    </Select>
                                                </FormControl>
                                                <FormMessage className="text-xs mt-1" />
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
                        onClick={() => append({ email: "", role: "organizer" })}
                        variant="secondary"
                    >
                        <Plus />Add member
                    </Button>
                </div>


                <Button type="submit">Add team</Button>

            </form>
        </Form>
    );
}
