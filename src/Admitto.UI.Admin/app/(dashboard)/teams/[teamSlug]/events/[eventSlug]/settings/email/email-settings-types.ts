import * as z from "zod";

export const emailSettingsSchema = z
    .object({
        smtpHost: z.string().min(1, "SMTP host is required"),
        smtpPort: z.coerce.number().int().min(1).max(65535),
        fromAddress: z.string().email(),
        authMode: z.enum(["none", "basic"]),
        username: z.string().optional(),
        password: z.string().optional(),
    })
    .refine((d) => d.authMode === "none" || (d.username && d.username.length > 0), {
        path: ["username"],
        message: "Username is required when auth mode is basic",
    });

export type EmailSettingsValues = z.infer<typeof emailSettingsSchema>;

export type EmailSettingsInitialValues = {
    smtpHost: string;
    smtpPort: number;
    fromAddress: string;
    authMode: "none" | "basic";
    username: string;
};
