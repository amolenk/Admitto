import {cookies} from "next/headers";

export async function GET(request: Request) {
    const url = new URL(request.url);
    const requestCookies = await cookies();
    const code = url.searchParams.get("code")!;
    const verifier = requestCookies.get("pkce_verifier")!.value;


    const tokenRes = await fetch("http://localhost:5100/connect/token", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
        },
        body: new URLSearchParams({
            client_id: "admitto-cli",
            client_secret: "admitto-secret",
            grant_type: "authorization_code",
            redirect_uri: "http://localhost:3000/api/auth/callback",
            code,
            code_verifier: verifier,
        }),
    }).then((r) => r.json());

    console.log("Token Response:", tokenRes);

    requestCookies.set("admitto_auth.access_token", tokenRes.access_token, {
        httpOnly: true,
        secure: true,
        sameSite: "lax",
        maxAge: 60 * 10, // 10 minutes (example)
    });

    requestCookies.set("admitto_auth.refresh_token", tokenRes.refresh_token, {
        httpOnly: true,
        secure: true,
        sameSite: "lax",
        maxAge: 60 * 60 * 24 * 30, // 30 days
    });

    return Response.redirect("http://localhost:3000/");
}