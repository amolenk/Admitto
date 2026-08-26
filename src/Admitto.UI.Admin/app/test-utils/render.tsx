import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, type RenderOptions, type RenderResult } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement, ReactNode } from "react";

/**
 * A QueryClient tuned for tests. The app's `QueryProvider` uses `throwOnError: true` and
 * `retry: 1`; both are wrong here — retries make failure assertions slow and flaky, and
 * throwing escapes to the error boundary instead of the component under test. Tests that
 * specifically cover error-boundary behaviour should build their own client.
 */
export function createTestQueryClient(): QueryClient {
    return new QueryClient({
        defaultOptions: {
            queries: { retry: false, throwOnError: false, gcTime: 0, staleTime: 0 },
            mutations: { retry: false },
        },
    });
}

/**
 * The provider tree as a wrapper component, for `renderHook` — which takes a `wrapper` but
 * has no equivalent of `renderWithProviders`.
 */
export function createQueryWrapper(queryClient: QueryClient = createTestQueryClient()) {
    return function Wrapper({ children }: { children: ReactNode }) {
        return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
    };
}

interface RenderWithProvidersOptions extends Omit<RenderOptions, "wrapper"> {
    queryClient?: QueryClient;
}

export interface RenderWithProvidersResult extends RenderResult {
    queryClient: QueryClient;
    user: ReturnType<typeof userEvent.setup>;
}

/**
 * Renders a client component inside the providers it expects at runtime, and returns a
 * `user` handle alongside the usual RTL result so tests don't each re-setup user-event.
 */
export function renderWithProviders(
    ui: ReactElement,
    { queryClient = createTestQueryClient(), ...options }: RenderWithProvidersOptions = {},
): RenderWithProvidersResult {
    const user = userEvent.setup();

    const result = render(ui, { wrapper: createQueryWrapper(queryClient), ...options });

    return { ...result, queryClient, user };
}
