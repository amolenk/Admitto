import { screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";
import { ScannerLinkCard } from "./scanner-link-card";

vi.mock("@/lib/api-client", () => ({ apiClient: { get: vi.fn(), post: vi.fn() } }));
const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);
const props = { teamId: "team", eventId: "event", isArchived: false, timeZone: "UTC" };

describe("scanner link card", () => {
    beforeEach(() => { post.mockResolvedValue({}); });
    afterEach(() => vi.clearAllMocks());

    // Given an event without a scanner link
    // When the scanner link card loads
    // Then it offers a create action
    it("noneState_offersCreateAction", async () => {
        get.mockResolvedValue({ status: "None", url: null, createdAt: null, expiresAt: null, revokedAt: null });
        const { user } = renderWithProviders(<ScannerLinkCard {...props} />);
        await screen.findByText("Create a link to share scanner access with your event team.");
        await user.click(screen.getByRole("button", { name: "Create scanner link" }));
        expect(post).toHaveBeenCalledWith("/api/teams/team/events/event/scanner-link");
    });

    // Given an active scanner link
    // When the card renders and the URL is copied
    // Then the URL is visible and the clipboard action is available
    it("activeState_showsUrlAndCopiesIt", async () => {
        get.mockResolvedValue({ status: "Active", url: "https://example.test/scan/secret", createdAt: "2027-06-12T18:00:00Z", expiresAt: "2027-06-12T20:00:00Z", revokedAt: null });
        const writeText = vi.spyOn(navigator.clipboard, "writeText").mockResolvedValue(undefined);
        const { user } = renderWithProviders(<ScannerLinkCard {...props} />);
        await screen.findByDisplayValue("https://example.test/scan/secret");
        await user.click(screen.getByRole("button", { name: "Copy scanner link" }));
        expect(writeText).toHaveBeenCalledWith("https://example.test/scan/secret");
    });

    // Given an active scanner link
    // When regenerate or revoke is selected
    // Then the mutation waits for explicit confirmation
    it("activeState_requiresConfirmationBeforeInvalidating", async () => {
        get.mockResolvedValue({ status: "Active", url: "https://example.test/scan/secret", createdAt: null, expiresAt: null, revokedAt: null });
        const { user } = renderWithProviders(<ScannerLinkCard {...props} />);
        await screen.findByDisplayValue("https://example.test/scan/secret");
        await user.click(screen.getByRole("button", { name: "Revoke" }));
        expect(post).not.toHaveBeenCalled();
        await user.click(screen.getByRole("button", { name: "Cancel" }));
        await user.click(screen.getByRole("button", { name: "Regenerate" }));
        expect(post).not.toHaveBeenCalled();
        await user.click(screen.getByRole("button", { name: "Regenerate link" }));
        await waitFor(() => expect(post).toHaveBeenCalledWith("/api/teams/team/events/event/scanner-link/regenerate"));
    });
});
