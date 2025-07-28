import { useState } from "react";
import { DefaultValues, FieldPath, FieldValues, useForm, UseFormReturn } from "react-hook-form";
import { FormError } from "@/components/form-error";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

interface UseCustomFormReturn<TFieldValues extends FieldValues> extends UseFormReturn<TFieldValues>
{
    generalError: { title: string; detail: string } | null;
    submit: (onValid: (values: TFieldValues) => Promise<void>) => (e?: React.BaseSyntheticEvent) => Promise<void>;
}

export function useCustomForm<TFieldValues extends FieldValues>(
    schema: z.ZodSchema<TFieldValues>,
    defaultValues: DefaultValues<TFieldValues>
): UseCustomFormReturn<TFieldValues>
{
    const [generalError, setGeneralError] = useState<{ title: string; detail: string } | null>(null);

    const form = useForm<TFieldValues>({
        resolver: zodResolver(schema),
        defaultValues,
        mode: "onChange"
    });

    const submit = (onValid: (values: TFieldValues) => Promise<void>) =>
        form.handleSubmit(
            async (values) =>
            {
                setGeneralError(null);
                try
                {
                    await onValid(values);
                }
                catch (error)
                {
                    if (error instanceof FormError)
                    {
                        setGeneralError({
                            title: error.title,
                            detail: error.detail
                        });

                        if (error.errors)
                        {
                            Object.entries(error.errors).forEach(([field, messages]) =>
                            {
                                form.setError(field as FieldPath<TFieldValues>, {
                                    type: "server",
                                    message: Array.isArray(messages) ? messages[0] : messages
                                });
                            });
                        }
                    }
                    else
                    {
                        setGeneralError({
                            title: "Unexpected Error",
                            detail: "An unexpected error occurred."
                        });
                    }
                }
            }
        );

    return { ...form, generalError, submit };
}
