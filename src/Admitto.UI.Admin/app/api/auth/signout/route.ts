export const runtime = "nodejs";

import { auth } from "@/lib/auth";
import { headers } from "next/headers";
import { NextResponse } from "next/server";

export async function GET(req: Request) {
    // Terminate the BetterAuth session server-side and capture the Set-Cookie
    // headers that clear the session cookie from the browser.
    const signOutResponse = await auth.api.signOut({
        headers: await headers(),
        asResponse: true,
    });

    // Construct the Keycloak RP-Initiated Logout URL so the IdP SSO session
    // is also destroyed, preventing automatic re-login on the next visit.
    const authority = process.env.AUTH_AUTHORITY ?? "";
    const clientId = process.env.AUTH_CLIENT_ID ?? "";
    const baseUrl = process.env.PUBLIC_BASE_URL ?? new URL(req.url).origin;

    const endSessionUrl = new URL(`${authority}/protocol/openid-connect/logout`);
    endSessionUrl.searchParams.set("client_id", clientId);
    endSessionUrl.searchParams.set("post_logout_redirect_uri", `${baseUrl}/signin`);

    const response = NextResponse.redirect(endSessionUrl.toString());

    const cookies = (signOutResponse.headers as any).getSetCookie?.() ?? [];
    for (const c of cookies) response.headers.append("set-cookie", c);

    return response;
}
