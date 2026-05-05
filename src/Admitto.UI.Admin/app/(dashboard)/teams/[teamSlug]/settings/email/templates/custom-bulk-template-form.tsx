"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle, Trash2 } from "lucide-react";
import * as z from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { CustomBulkTemplateDto } from "@/lib/admitto-api/generated";

const formSchema = z.object({
    name: z.string().min(1, "Name is required").max(200),
    subject: z.string().min(1, "Subject is required").max(500),
    textBody: z.string().min(1, "Text body is required"),
    htmlBody: z.string().optional(),
});

type FormValues = z.infer<typeof formSchema>;

export function CustomBulkTemplateForm({
    template,
    apiUrl,
    queryKey,
    backHref,
}: {
    template: CustomBulkTemplateDto;
    apiUrl: string;
    queryKey: unknown[];
    backHref: string;
}) {
    const router = useRouter();
    const queryClient = useQueryClient();
    const [previewHtml, setPreviewHtml] = useState<string>(template.htmlBody ?? "");

    const form = useCustomForm<FormValues>(formSchema, {
        name: template.name,
        subject: template.subject,
        textBody: template.textBody,
        htmlBody: template.htmlBody ?? "",
    });

    const onSubmit = form.submit(async (values) => {
        await apiClient.put(apiUrl, {
            name: values.name,
            subject: values.subject,
            textBody: values.textBody,
            htmlBody: values.htmlBody || null,
            version: template.version,
        });
        queryClient.invalidateQueries({ queryKey });
    });

    async function handleDelete() {
        await apiClient.delete(apiUrl);
        queryClient.invalidateQueries({ queryKey: queryKey.slice(0, -1) });
        router.push(backHref);
    }

    const htmlBodyValue = form.watch("htmlBody");

    return (
        <Form {...form}>
            <form onSubmit={onSubmit} className="space-y-6">
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
                    </Alert>
                )}

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div className="space-y-4">
                        <FormField control={form.control} name="name" render={({ field }) => (
                            <FormItem>
                                <FormLabel>Template name</FormLabel>
                                <FormControl>
                                    <Input placeholder="e.g. Alumni invitation" {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />

                        <FormField control={form.control} name="subject" render={({ field }) => (
                            <FormItem>
                                <FormLabel>Subject</FormLabel>
                                <FormControl>
                                    <Input placeholder="Email subject line" {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />

                        <FormField control={form.control} name="textBody" render={({ field }) => (
                            <FormItem>
                                <FormLabel>Text body</FormLabel>
                                <FormControl>
                                    <Textarea placeholder="Plain-text email body" rows={8} {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />

                        <FormField control={form.control} name="htmlBody" render={({ field }) => (
                            <FormItem>
                                <FormLabel>
                                    HTML body{" "}
                                    <span className="text-muted-foreground font-normal">(optional)</span>
                                </FormLabel>
                                <FormControl>
                                    <Textarea
                                        placeholder="HTML email body"
                                        rows={10}
                                        {...field}
                                        onChange={(e) => {
                                            field.onChange(e);
                                            setPreviewHtml(e.target.value);
                                        }}
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                    </div>

                    <div className="flex flex-col">
                        <div className="text-[13.5px] font-medium mb-2">HTML preview</div>
                        {htmlBodyValue ? (
                            <iframe
                                className="flex-1 w-full border rounded-md bg-white"
                                style={{ minHeight: "400px" }}
                                srcDoc={previewHtml}
                                sandbox=""
                                title="HTML preview"
                            />
                        ) : (
                            <div className="flex-1 border rounded-md flex items-center justify-center text-[13px] text-muted-foreground bg-muted/30" style={{ minHeight: "400px" }}>
                                Enter HTML body to see preview
                            </div>
                        )}
                    </div>
                </div>

                <div className="flex items-center justify-between pt-2 border-t">
                    <AlertDialog>
                        <AlertDialogTrigger asChild>
                            <Button type="button" variant="outline" className="text-destructive hover:text-destructive">
                                <Trash2 className="size-3.5 mr-1.5" />
                                Delete template
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Delete template?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    &quot;{template.name}&quot; will be permanently deleted. This cannot be undone.
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <AlertDialogAction
                                    onClick={handleDelete}
                                    className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                                >
                                    Delete
                                </AlertDialogAction>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>

                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Saving…" : "Save changes"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}
