import {betterAuth} from "better-auth";
import {genericOAuth} from "better-auth/plugins"
import {Pool} from "pg";

export const auth = betterAuth({
    baseURL: process.env.PUBLIC_BASE_URL, // Set public URL when behind a proxy
    database: new Pool({
        connectionString: process.env.BETTER_AUTH_DB
    }),
    experimental: {joins: true},
    plugins: [
        genericOAuth({
            config: [
                {
                    providerId: "generic-oauth",
                    discoveryUrl: `${process.env.AUTH_AUTHORITY || ""}/.well-known/openid-configuration`,
                    clientId: process.env.AUTH_CLIENT_ID || "",
                    clientSecret: process.env.AUTH_CLIENT_SECRET || "",
                    scopes: (process.env.AUTH_SCOPES || "").split(" "),
                    prompt: process.env.AUTH_PROMPT as "select_account" | "login" || "select_account",
                }
            ]
        }),
    ],
    cookies: {
        state: {
            attributes: {
                sameSite: "none",
                secure: true,
            }
        }
    },
});

export type Session = typeof auth.$Infer.Session
