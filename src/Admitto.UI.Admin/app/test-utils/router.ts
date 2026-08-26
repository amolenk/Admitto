import { vi } from "vitest";

/**
 * Backing store for the `next/navigation` mock installed in ./setup.ts.
 *
 * Tests assert navigation through `routerMock.push` and set up route context with
 * `setRoute({ params: { teamId, eventId } })` — every client page reads its ids from
 * `useParams()`, so most component tests need at least that.
 */
export const routerMock = {
    push: vi.fn(),
    replace: vi.fn(),
    back: vi.fn(),
    forward: vi.fn(),
    refresh: vi.fn(),
    prefetch: vi.fn(),
};

export const redirectMock = vi.fn();
export const notFoundMock = vi.fn();

const defaults = {
    params: {} as Record<string, string>,
    pathname: "/",
    searchParams: new URLSearchParams(),
};

let route = { ...defaults };

export const navigationMock = {
    useRouter: () => routerMock,
    useParams: () => route.params,
    usePathname: () => route.pathname,
    useSearchParams: () => route.searchParams,
    redirect: redirectMock,
    notFound: notFoundMock,
};

export function setRoute(next: {
    params?: Record<string, string>;
    pathname?: string;
    searchParams?: URLSearchParams | Record<string, string>;
}): void {
    if (next.params) route.params = next.params;
    if (next.pathname) route.pathname = next.pathname;
    if (next.searchParams) {
        route.searchParams =
            next.searchParams instanceof URLSearchParams
                ? next.searchParams
                : new URLSearchParams(next.searchParams);
    }
}

export function resetRouterMocks(): void {
    route = { ...defaults, searchParams: new URLSearchParams() };
}
