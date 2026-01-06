import crypto from "crypto";
import {cookies} from "next/headers";

export async function GET() {
    const verifier = crypto.randomBytes(32).toString("base64url");
    const state = crypto.randomBytes(16).toString("hex");

    (await cookies()).set("pkce_verifier", verifier, {
        httpOnly: true,
        secure: true,
        sameSite: "lax",
    });

    const challenge = base64URLEncode(sha256(verifier));

    const authorizeUrl = new URL("http://localhost:5100/connect/authorize");
    authorizeUrl.searchParams.set("client_id", "admitto-cli");
    authorizeUrl.searchParams.set("response_type", "code");
    authorizeUrl.searchParams.set("redirect_uri", "http://localhost:3000/api/auth/callback");
    authorizeUrl.searchParams.set("scope", "openid profile email offline_access admin");
    authorizeUrl.searchParams.set("state", state);
    authorizeUrl.searchParams.set("code_challenge", challenge);
    authorizeUrl.searchParams.set("code_challenge_method", "S256");

    return Response.redirect(authorizeUrl.toString());
}

function sha256(input: string) {
    return crypto.createHash("sha256").update(input).digest();
}

function base64URLEncode(buffer: Buffer) {
    return buffer.toString("base64url");
}