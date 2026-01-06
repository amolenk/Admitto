import { cookies } from "next/headers";
import {NextRequest, NextResponse} from "next/server";
import {
    AUTHORIZE_URL,
    CLIENT_ID,
    CLIENT_SECRET,
    OAUTH_CALLBACK_URL,
    PKCE_VERIFIER_COOKIE,
} from "@/lib/auth/config";
import { generatePkceVerifier, generatePkceChallenge } from "@/lib/auth/pkce";

//export const runtime = "nodejs";

export async function GET(req: NextRequest) {

    const url = new URL(req.url);


    const cookieStore = await cookies();

    const verifier = generatePkceVerifier();
    const challenge = generatePkceChallenge(verifier);

    cookieStore.set(PKCE_VERIFIER_COOKIE, verifier, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
    });

    const authorizeUrl = new URL(AUTHORIZE_URL);
    authorizeUrl.searchParams.set("client_id", CLIENT_ID);
    // authorizeUrl.searchParams.set("client_secret", CLIENT_SECRET);
    authorizeUrl.searchParams.set("response_type", "code");
    authorizeUrl.searchParams.set("redirect_uri", OAUTH_CALLBACK_URL);
    authorizeUrl.searchParams.set("scope", "openid profile email offline_access admin");
    authorizeUrl.searchParams.set("code_challenge", challenge);
    authorizeUrl.searchParams.set("code_challenge_method", "S256");

    const rawReturn = url.searchParams.get("return_url") ?? undefined;
    if (rawReturn)
    {
        authorizeUrl.searchParams.set("return_url", rawReturn);
    }

    // The user is already authenticated at the auth server (via magic link),
    // so /authorize should not show any extra UI and just redirect back.
    return NextResponse.redirect(authorizeUrl.toString());
}