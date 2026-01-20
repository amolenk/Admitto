import {betterAuth} from "better-auth";
import {genericOAuth} from "better-auth/plugins"
import {Pool} from "pg";

export const auth = betterAuth({
    database: new Pool({
        connectionString: process.env.NEXT_BETTER_AUTH
    }),
    experimental: {joins: true},
    plugins: [
        genericOAuth({
            config: [
                {
                    providerId: "generic-oauth",
                    discoveryUrl: `${process.env.BETTER_AUTH_AUTHORITY || ""}/.well-known/openid-configuration`,
                    clientId: process.env.BETTER_AUTH_CLIENT_ID || "",
                    clientSecret: process.env.BETTER_AUTH_CLIENT_SECRET || "",
                    scopes: (process.env.BETTER_AUTH_SCOPES || "").split(" "),
                    prompt: process.env.BETTER_AUTH_PROMPT || "select_account",
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