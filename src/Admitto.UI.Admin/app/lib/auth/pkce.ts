import crypto from "crypto";

export function generatePkceVerifier(): string {
    return base64UrlEncode(crypto.randomBytes(32));
}

export function generatePkceChallenge(verifier: string): string {
    const hash = crypto.createHash("sha256").update(verifier).digest();
    return base64UrlEncode(hash);
}

function base64UrlEncode(buffer: Buffer): string {
    return buffer
        .toString("base64")
        .replace(/\+/g, "-")
        .replace(/\//g, "_")
        .replace(/=+$/g, "");
}