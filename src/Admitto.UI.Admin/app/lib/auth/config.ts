export const AUTH_SERVER_URL =
    process.env.AUTH_SERVER_URL ?? "http://localhost:5100";

export const CLIENT_ID = process.env.ADMITTO_CLIENT_ID ?? "admitto-dashboard";
export const CLIENT_SECRET = process.env.ADMITTO_CLIENT_SECRET ?? "admitto-secret"; // TODO

// Public base URL of your Next.js app
export const APP_BASE_URL =
    process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3000";

export const AUTHORIZE_URL = `${AUTH_SERVER_URL}/auth/authorize`;
export const TOKEN_URL = `${AUTH_SERVER_URL}/auth/token`;
export const MAGIC_LINK_URL = `${AUTH_SERVER_URL}/auth/magic-link`;
export const DEVICE_VERIFY_URL = `${AUTH_SERVER_URL}/auth/verify`;

export const LOGIN_PAGE_URL = `${APP_BASE_URL}/login`;

// Where the auth server redirects after magic-link verification
export const MAGIC_LINK_CALLBACK_URL = `${APP_BASE_URL}/login/callback`;

// Where the auth server redirects after OAuth /authorize
export const OAUTH_CALLBACK_URL = `${APP_BASE_URL}/api/auth/authorize/callback`;

// Cookie names
export const ACCESS_TOKEN_COOKIE = "admitto_auth.access_token";
export const REFRESH_TOKEN_COOKIE = "admitto_auth.refresh_token";
export const PKCE_VERIFIER_COOKIE = "admitto_auth.pkce_verifier";

export const PUBLIC_ROUTES = [
    "/login",
    "/device",
    "/api/auth"
];