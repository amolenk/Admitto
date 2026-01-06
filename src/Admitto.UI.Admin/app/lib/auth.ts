import { betterAuth } from "better-auth";
import { genericOAuth, microsoftEntraId } from "better-auth/plugins"
import { Pool } from "pg";

export const auth = betterAuth({
    database: new Pool({
        connectionString: ""
    }),
    experimental: { joins: true },
    plugins: [
        genericOAuth({
            config: [

            ]
        }),
    ],
    // account: {
    //     accountLinking: {
    //         enabled: false,
    //         allowDifferentEmails: true
    //     },
    // },
    advanced: {
        cookies: {
            state: {
                attributes: {
                    sameSite: "none",
                    secure: true,
                }
            }
        },
        // useSecureCookies: true,
        // crossSubDomainCookies: {
        //     enabled: false,
        // },
        // defaultCookieAttributes: {
        //     sameSite: "none",
        //     secure: true,
        //     domain: undefined,
        //     path: "/",
        // },
        // cookiePrefix: "better_auth",
        // disableCSRFCheck: true,
    },
});



// providerId: 'okta',
//     clientId: process.env.AUTH_OKTA_ID as string,
//     clientSecret: process.env.AUTH_OKTA_SECRET as string,
//     scopes: ['openid', 'profile', 'email', 'offline_access', 'groups'],
//     discoveryUrl: `${process.env.AUTH_OKTA_ISSUER}/.well-known/openid-configuration`,
//     accessType: 'offline',




export type Session = typeof auth.$Infer.Session
