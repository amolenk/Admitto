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
                    discoveryUrl: `${process.env.BETTER_AUTH_AUTHORITY || (() => {
                        throw new Error("BETTER_AUTH_DISCOVERY_URL not set")
                    })()}/.well-known/openid-configuration`,
                    clientId: process.env.BETTER_AUTH_CLIENT_ID || (() => {
                        throw new Error("BETTER_AUTH_CLIENT_ID not set")
                    })(),
                    clientSecret: process.env.BETTER_AUTH_CLIENT_SECRET || (() => {
                        throw new Error("BETTER_AUTH_CLIENT_SECRET not set")
                    })(),
                    scopes: (process.env.BETTER_AUTH_SCOPES || (() => {
                        throw new Error("BETTER_AUTH_SCOPES not set")
                    })()).split(" "),
                    prompt: process.env.BETTER_AUTH_PROMPT as "select_account" | "login" | undefined || (() => {
                        throw new Error("BETTER_AUTH_PROMPT not set")
                    })(),
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