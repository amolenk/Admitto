import { NextRequest, NextResponse } from "next/server";
import { auth } from "@/lib/auth";

export async function GET(req: NextRequest) {
    const params = req.nextUrl.searchParams;
    const returnUrl = params.get("return_url") ?? "/";
//    const returnUrl = decodeURIComponent(Array.isArray(raw) ? raw[0] : raw ?? "/");

    const response = await auth.api.signInWithOAuth2({
        body: {
            providerId: "admitto",
            callbackURL: returnUrl
        },
        headers: req.headers,
        asResponse: true,
    });

    const redirectInfo = (await response.json()) as {
        url: string;
        redirect: boolean;
    };

    if (redirectInfo.redirect) {
        const redirectUrl = new URL(redirectInfo.url);
        // set or overwrite return_url query param to the full returnUrl
        redirectUrl.searchParams.set("return_url", returnUrl);

        const redirectHeaders = new Headers();
        redirectHeaders.set('Location', redirectUrl.toString());
        redirectHeaders.set('Set-Cookie', response.headers.get('Set-Cookie')!);
        const redirectResponse = new Response(null, {
            status: 302,
            headers: redirectHeaders,
        });

        return redirectResponse;
    }


    return NextResponse.json({});// ok: true, magicLinkUrl: magicLink });
}