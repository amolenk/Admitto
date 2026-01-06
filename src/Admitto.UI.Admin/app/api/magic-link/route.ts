import { NextRequest, NextResponse } from "next/server";
import { MAGIC_LINK_URL, MAGIC_LINK_CALLBACK_URL, CLIENT_ID } from "@/lib/auth/config";

// export const runtime = "nodejs";

export async function POST(req: NextRequest) {
    const { email, returnUrl } = await req.json().catch(() => ({}));

    if (!email || typeof email !== "string") {
        return NextResponse.json({ error: "Email is required" }, { status: 400 });
    }

    if (!returnUrl || typeof returnUrl !== "string") {
        return NextResponse.json({ error: "Return URL is required" }, { status: 400 });
    }

    const res = await fetch(MAGIC_LINK_URL, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            Email: email,
            ReturnUrl: returnUrl,
            ClientId: CLIENT_ID
        }),
    });

    const body = await res.json().catch(() => ({}));
    const magicLink = body.MagicLinkUrl ?? body.magicLinkUrl ?? body.magicLink ?? null;

    return NextResponse.json({ ok: true, magicLinkUrl: magicLink });
}