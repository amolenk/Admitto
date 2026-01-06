'use client'

import {Button} from "@/components/ui/button"
import {Card, CardContent, CardDescription, CardHeader, CardTitle,} from "@/components/ui/card"
import {Field, FieldGroup, FieldLabel, FieldSeparator,} from "@/components/ui/field"
import {Input} from "@/components/ui/input"
import {Spinner} from "@/components/ui/spinner";
import {cn} from "@/lib/utils"
import { FormEvent, useState } from "react";

type Props = React.ComponentProps<"div"> & {
    returnUrl?: string;
};

export function SignInForm({
                              className,
                              returnUrl,
                              ...props
                          }: Props) {

    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);
    const [sent, setSent] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function onSubmit(e: FormEvent) {
        e.preventDefault();
        setError(null);
        setLoading(true);

        try {
            const res = await fetch("/api/magic-link", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, returnUrl }),
            });

            if (res.ok) {
                const body: any = await res.json().catch(() => ({}));
                setSent(true);
            } else {
                const body = await res.json().catch(() => ({}));
                setError(body.error ?? "Failed to send magic link");
            }
        }
        catch {
            setError("An unexpected error occurred");
        }
        finally {
            setLoading(false);
        }
    }

    if (sent) {
        return <>
            <h1 className="text-lg font-semibold">Check your email</h1>
            <p>Click the magic link we just sent to finish signing in.</p>
        </>
    }

    return (
        <div className={cn("flex flex-col gap-6", className)} {...props}>
            <Card>
                <CardHeader className="text-center">
                    <CardTitle className="text-xl">Admitto</CardTitle>
                    <CardDescription>
                        Login with your registered email address
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <form onSubmit={onSubmit}>
                        <FieldGroup>
                            <FieldSeparator className="*:data-[slot=field-separator-content]:bg-card">
                            </FieldSeparator>
                            <Field>
                                <FieldLabel htmlFor="email">Email</FieldLabel>
                                <Input
                                    id="email"
                                    type="email"
                                    placeholder="you@example.com"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    required
                                />
                            </Field>
                            <Field>
                                <Button type="submit" disabled={loading}>
                                    {loading ? (
                                        <>
                                            <Spinner />
                                            Sending Magic Link...
                                        </>
                                    ) : "Send Magic Link"}
                                </Button>
                                {error && <p style={{ color: "red" }}>{error}</p>}
                            </Field>
                        </FieldGroup>
                    </form>
                </CardContent>
            </Card>
        </div>
    )
}
