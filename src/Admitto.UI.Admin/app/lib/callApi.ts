'use client';

import { useRouter } from "next/navigation";
import { redirect } from "next/navigation";

export function useApi() {
    const router = useRouter();

    async function fetchApi<T>(path: string, init?: RequestInit): Promise<T | null> {
        const res = await fetch(path, init);

        if (res.status === 401) {
            router.push("/signin");
            return null;
        }

        return (await res.json()) as T;
    }

    return fetchApi;
}


export async function serverApi<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(process.env.NEXT_PUBLIC_BASE_URL + path, {
        cache: "no-store",
        ...init,
    });

    if (res.status === 401) {
        redirect("/signin");
    }

    return res.json();
}