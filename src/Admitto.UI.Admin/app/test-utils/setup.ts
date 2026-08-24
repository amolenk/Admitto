import "@testing-library/jest-dom/vitest";

import { cleanup } from "@testing-library/react";
import { afterEach, beforeEach, vi } from "vitest";

import { resetRouterMocks } from "./router";

// Every client page reads route params through next/navigation, so the whole module is
// mocked here. Per-test control lives in ./router.
vi.mock("next/navigation", async () => {
    const { navigationMock } = await import("./router");
    return navigationMock;
});

// next/font/google runs at module scope in the layouts and expects a build-time loader.
vi.mock("next/font/google", () => {
    const font = () => ({ variable: "--font-mock", className: "font-mock", style: {} });
    return new Proxy({}, { get: () => font });
});

beforeEach(() => {
    // jsdom implements none of these, and Radix/next-themes/use-mobile all reach for them.
    // Without matchMedia, next-themes throws on mount; without ResizeObserver, every
    // Radix popper-based component (Select, Popover, Tooltip) throws.
    vi.stubGlobal(
        "matchMedia",
        vi.fn((query: string) => ({
            matches: false,
            media: query,
            onchange: null,
            addListener: vi.fn(),
            removeListener: vi.fn(),
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            dispatchEvent: vi.fn(),
        })),
    );

    vi.stubGlobal(
        "ResizeObserver",
        vi.fn(() => ({ observe: vi.fn(), unobserve: vi.fn(), disconnect: vi.fn() })),
    );

    vi.stubGlobal(
        "IntersectionObserver",
        vi.fn(() => ({
            observe: vi.fn(),
            unobserve: vi.fn(),
            disconnect: vi.fn(),
            takeRecords: vi.fn(() => []),
        })),
    );

    // Radix guards pointer interactions with these three. jsdom ships none of them, and
    // the failure mode is an opaque "not a function" inside a Radix internal.
    Element.prototype.scrollIntoView = vi.fn();
    Element.prototype.hasPointerCapture = vi.fn(() => false);
    Element.prototype.releasePointerCapture = vi.fn();
    Element.prototype.setPointerCapture = vi.fn();
});

afterEach(() => {
    cleanup();
    resetRouterMocks();
    vi.unstubAllGlobals();
    vi.clearAllMocks();
});
