"use client";

import { useEffect } from "react";
import { signIn } from "next-auth/react";

export default function SignInPage() {
    useEffect(() => {
        signIn("keycloak", { redirectTo: "/" });
    }, []);

    return <p>Redirecting to Keycloak...</p>;
}