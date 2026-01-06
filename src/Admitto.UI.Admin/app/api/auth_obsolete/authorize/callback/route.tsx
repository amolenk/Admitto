import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";
import {
    TOKEN_URL,
    CLIENT_ID,
    CLIENT_SECRET,
    OAUTH_CALLBACK_URL,
    PKCE_VERIFIER_COOKIE,
    ACCESS_TOKEN_COOKIE,
    REFRESH_TOKEN_COOKIE,
} from "@/lib/auth/config";

// export const runtime = "nodejs";

export async function GET(req: NextRequest) {
    const url = new URL(req.url);
    const code = url.searchParams.get("code");

    if (!code) {
        return NextResponse.json({ error: "Missing authorization code" }, { status: 400 });
    }

    const cookieStore = await cookies();
    const verifier = cookieStore.get(PKCE_VERIFIER_COOKIE)?.value;

    if (!verifier) {
        return NextResponse.json({ error: "Missing PKCE verifier" }, { status: 400 });
    }

    // Exchange code for tokens
    const tokenRes = await fetch(TOKEN_URL, {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
        },
        body: new URLSearchParams({
            grant_type: "authorization_code",
            client_id: CLIENT_ID,
            client_secret: CLIENT_SECRET,
            code,
            redirect_uri: OAUTH_CALLBACK_URL,
            code_verifier: verifier,
        }),
    });

    if (!tokenRes.ok) {
        const body = await tokenRes.json().catch(() => ({}));
        console.error("Token exchange failed:", body);
        return NextResponse.json({ error: "Token exchange failed" }, { status: 500 });
    }

    const tokens = await tokenRes.json();

    const accessToken = tokens.access_token as string | undefined;
    const refreshToken = tokens.refresh_token as string | undefined;
    const expiresIn = tokens.expires_in as number | undefined;

    if (!accessToken) {
        return NextResponse.json({ error: "No access token returned" }, { status: 500 });
    }

    // Store tokens in HttpOnly cookies
    cookieStore.set(ACCESS_TOKEN_COOKIE, accessToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        maxAge: expiresIn ?? 600,
    });

    if (refreshToken) {
        cookieStore.set(REFRESH_TOKEN_COOKIE, refreshToken, {
            httpOnly: true,
            secure: process.env.NODE_ENV === "production",
            sameSite: "lax",
            path: "/",
            maxAge: 60 * 60 * 24 * 30, // 30 days
        });
    }

    // Clean up PKCE verifier
    cookieStore.delete(PKCE_VERIFIER_COOKIE);

    const rawReturn = url.searchParams.get("return_url") ?? "/";

    console.log(process.env.NEXT_PUBLIC_APP_URL);

    return NextResponse.redirect(rawReturn);
}