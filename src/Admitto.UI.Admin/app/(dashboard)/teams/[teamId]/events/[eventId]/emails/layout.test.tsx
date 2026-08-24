import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import EmailsLayout from "./layout";

// Harvested from openspec `admin-ui-bulk-emails`. The bulk-emails rework dropped the
// Templates and Setup tabs, leaving Campaigns as the only tab; that's the code's
// authoritative shape per TESTING-BACKLOG.md.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";

describe("EmailsLayout", () => {
    beforeEach(() => {
        setRoute({
            params: { teamId: TEAM_ID, eventId: EVENT_ID },
            pathname: `/teams/${TEAM_ID}/events/${EVENT_ID}/emails/campaigns`,
        });
        get.mockResolvedValue({ name: "DevConf 2026" });
    });

    // Given the bulk-emails feature no longer has Templates or Setup views
    // When the emails layout renders its tab bar
    // Then Campaigns is the only tab available
    it("shows only the Campaigns tab, with Templates and Setup removed", async () => {
        renderWithProviders(
            <EmailsLayout>
                <div>campaigns content</div>
            </EmailsLayout>,
        );

        expect(await screen.findByRole("link", { name: "Campaigns" })).toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Templates" })).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Setup" })).not.toBeInTheDocument();
    });
});
