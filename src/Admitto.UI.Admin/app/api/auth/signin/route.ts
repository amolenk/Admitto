export const runtime = "nodejs";

import { auth } from "@/lib/auth";
import { headers } from "next/headers";
import { NextResponse } from "next/server";

export async function GET(req: Request) {
    const response = await auth.api.signInWithOAuth2({
        body: { providerId: process.env.AUTH_PROVIDER_ID as string || "keycloak" },
        headers: await headers(),
        asResponse: true,
    });

    const { url, redirect } = (await response.json()) as { url: string; redirect: boolean };
    if (!redirect) return NextResponse.json({ error: "Expected redirect" }, { status: 500 });

    const next = NextResponse.redirect(url);

    // TODO Check that we still need this cookie stuff for the state cookie
    // Forward *all* Set-Cookie headers
    const cookies = (response.headers as any).getSetCookie?.() ?? [];
    for (const c of cookies) next.headers.append("set-cookie", c);

    return next;
}