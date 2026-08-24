import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import { NavLinks } from "./nav-links";

// Harvested from the navigation scenarios in openspec `admin-ui-team-api-keys` (SC101/SC102),
// `admin-ui-team-membership` and `admin-ui-team-danger-zone`, which each assert that their tab
// is present in the settings sidebar and points at the right path.

const BASE = "/teams/acme/settings";

function renderNav(pathname: string) {
    setRoute({ pathname });
    return renderWithProviders(<NavLinks basePath={BASE} />);
}

/** The `aria-current`-free active style is the only signal, so match on it directly. */
function isActive(link: HTMLElement) {
    return link.className.includes("bg-card");
}

describe("settings NavLinks", () => {
    // Given the settings sidebar
    // When it renders
    // Then every settings area is reachable, each under the team's settings base path
    it.each([
        ["General", ""],
        ["Members", "/members"],
        ["API Keys", "/api-keys"],
        ["Danger zone", "/danger"],
    ])("links %s to the settings path", (label, suffix) => {
        renderNav(BASE);

        expect(screen.getByRole("link", { name: new RegExp(label) })).toHaveAttribute(
            "href",
            `${BASE}${suffix}`,
        );
    });

    // Given the organizer is on the settings root
    // When the nav renders
    // Then General is the active entry
    it("marks General active on the settings root", () => {
        renderNav(BASE);

        expect(isActive(screen.getByRole("link", { name: /General/ }))).toBe(true);
    });

    // Given the organizer is on a settings sub-page
    // When the nav renders
    // Then General is no longer active — it matches its own path exactly, so it does not
    // stay highlighted alongside the sub-page's own entry
    it("does not keep General active on a sub-page", () => {
        renderNav(`${BASE}/members`);

        expect(isActive(screen.getByRole("link", { name: /General/ }))).toBe(false);
        expect(isActive(screen.getByRole("link", { name: /Members/ }))).toBe(true);
    });

    // Given a path nested below a settings entry
    // When the nav renders
    // Then that entry is still active, so deep pages do not lose their sidebar context
    it("keeps an entry active on paths nested below it", () => {
        renderNav(`${BASE}/api-keys/some-key`);

        expect(isActive(screen.getByRole("link", { name: /API Keys/ }))).toBe(true);
    });

    // Given a path that merely starts with the same characters as an entry
    // When the nav renders
    // Then that entry is not active, because matching is on path segments
    it("does not activate an entry on a merely prefix-matching path", () => {
        renderNav(`${BASE}/api-keys-archive`);

        expect(isActive(screen.getByRole("link", { name: /API Keys/ }))).toBe(false);
    });
});
