import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { apiKeyDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import ApiKeysPage from "./page";

// Harvested from openspec `admin-ui-team-api-keys` (SC103-SC111). As on the Members page,
// the spec's "validation error when name is empty" is implemented as a disabled button.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);
const del = vi.mocked(apiClient.delete);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";

const activeKey = apiKeyDto({
    id: "key-active",
    name: "Production",
    keyPrefix: "adm_live_abcd",
    createdAt: "2026-03-01T10:00:00Z",
    createdBy: "owner@example.com",
});
const revokedKey = apiKeyDto({
    id: "key-revoked",
    name: "Old staging",
    keyPrefix: "adm_test_wxyz",
    revokedAt: "2026-04-02T10:00:00Z",
});

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID } });
    return renderWithProviders(<ApiKeysPage />);
}

/** The row for a key, so per-key controls can be scoped. */
function keyRow(name: string) {
    return screen.getByText(name).closest("div.flex.items-center") as HTMLElement;
}

describe("ApiKeysPage list", () => {
    // Given a team with one active and one revoked key
    // When the page loads
    // Then each row shows the name, the prefix, who created it and when — and the status
    // distinguishes active keys (badge) from revoked ones (revocation date)
    it("lists keys with their prefix, creator and status", async () => {
        get.mockResolvedValue([activeKey, revokedKey]);

        renderPage();

        expect(await screen.findByText("Production")).toBeInTheDocument();
        const active = keyRow("Production");
        expect(within(active).getByText(/adm_live_abcd/)).toBeInTheDocument();
        expect(within(active).getByText(/owner@example\.com/)).toBeInTheDocument();
        expect(within(active).getByText(/2026/)).toBeInTheDocument();
        expect(within(active).getByText("Active")).toBeInTheDocument();

        expect(within(keyRow("Old staging")).getByText(/^Revoked /)).toBeInTheDocument();
    });

    // Given the raw key value is never returned by the list endpoint
    // When the list renders
    // Then only the prefix is shown — the full key is not recoverable from this page
    it("shows only the key prefix, never a full key", async () => {
        get.mockResolvedValue([activeKey]);

        renderPage();
        await screen.findByText("Production");

        expect(screen.queryByText(/adm_live_abcd[a-z0-9]/i)).not.toBeInTheDocument();
    });

    // Given a team with no keys
    // When the page loads
    // Then an empty state invites creating the first one
    it("shows an empty state when the team has no keys", async () => {
        get.mockResolvedValue([]);

        renderPage();

        expect(await screen.findByText("No API keys yet")).toBeInTheDocument();
    });

    // Given a mix of active and revoked keys
    // When the list renders
    // Then only the active key offers a Revoke action — revoking twice is meaningless
    it("offers Revoke only for active keys", async () => {
        get.mockResolvedValue([activeKey, revokedKey]);

        renderPage();
        await screen.findByText("Production");

        expect(within(keyRow("Production")).getByRole("button", { name: "Revoke" })).toBeInTheDocument();
        expect(within(keyRow("Old staging")).queryByRole("button", { name: "Revoke" })).toBeNull();
    });
});

describe("ApiKeysPage create", () => {
    const created = { id: "key-new", name: "Staging", keyPrefix: "adm_test_1234", key: "adm_test_1234_secret" };

    beforeEach(() => {
        get.mockResolvedValue([]);
        post.mockResolvedValue(created);
    });

    // Given a name for the new key
    // When it is created
    // Then the raw key is shown once, with a warning that it cannot be retrieved again
    it("shows the raw key once with a warning after creation", async () => {
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "Staging");
        await user.click(screen.getByRole("button", { name: /create key/i }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/api-keys`, { name: "Staging" }),
        );
        expect(await screen.findByText("API key created")).toBeInTheDocument();
        expect(screen.getByText(created.key)).toBeInTheDocument();
        expect(screen.getByText("Store this key securely")).toBeInTheDocument();
    });

    // Given the one-time dialog is showing the raw key
    // When the copy button is pressed
    // Then the full key is on the clipboard — the only way to capture it
    it("copies the raw key to the clipboard", async () => {
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "Staging");
        await user.click(screen.getByRole("button", { name: /create key/i }));
        await screen.findByText("API key created");

        const dialog = screen.getByRole("dialog");
        const buttons = within(dialog).getAllByRole("button");
        await user.click(buttons.find((b) => b.textContent === "")!);

        await expect(navigator.clipboard.readText()).resolves.toBe(created.key);
    });

    // Given the one-time dialog is open
    // When it is dismissed
    // Then the raw key is gone from the page and the name field is empty for the next key
    it("hides the raw key once the dialog is dismissed", async () => {
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "Staging");
        await user.click(screen.getByRole("button", { name: /create key/i }));
        await screen.findByText("API key created");

        await user.click(screen.getByRole("button", { name: "Done" }));

        await waitFor(() => expect(screen.queryByText(created.key)).not.toBeInTheDocument());
        expect(screen.getByPlaceholderText("e.g. Production, Staging")).toHaveValue("");
    });

    // Given an empty or whitespace-only name
    // When the owner looks at the form
    // Then Create is disabled, so no unnamed key is created
    it("disables Create until a non-blank name is entered", async () => {
        const { user } = renderPage();
        const create = screen.getByRole("button", { name: /create key/i });

        expect(create).toBeDisabled();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "   ");
        expect(create).toBeDisabled();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "Staging");
        expect(create).toBeEnabled();
    });

    // Given a valid name typed in the field
    // When Enter is pressed instead of clicking Create
    // Then the key is created, matching the button
    it("creates the key when Enter is pressed", async () => {
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "Staging{Enter}");

        await waitFor(() => expect(post).toHaveBeenCalledTimes(1));
    });

    // Given the backend rejects the name
    // When creation fails
    // Then the reason is shown and no one-time dialog appears
    it("shows the backend's reason when creation fails", async () => {
        post.mockRejectedValue(
            new FormError({ status: 400, title: "Bad Request", detail: "A key with that name exists." }),
        );
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("e.g. Production, Staging"), "Production");
        await user.click(screen.getByRole("button", { name: /create key/i }));

        expect(await screen.findByText("A key with that name exists.")).toBeInTheDocument();
        expect(screen.queryByText("API key created")).not.toBeInTheDocument();
    });
});

describe("ApiKeysPage revoke", () => {
    beforeEach(() => {
        get.mockResolvedValue([activeKey]);
        del.mockResolvedValue(undefined);
    });

    // Given an active key
    // When Revoke is clicked
    // Then a confirmation warns that integrations break immediately, and nothing is sent yet
    it("warns before revoking and sends nothing until confirmed", async () => {
        const { user } = renderPage();
        await screen.findByText("Production");

        await user.click(screen.getByRole("button", { name: "Revoke" }));

        expect(await screen.findByText("Revoke API key")).toBeInTheDocument();
        expect(screen.getByText(/stop working right away/)).toBeInTheDocument();
        expect(del).not.toHaveBeenCalled();
    });

    // Given the revoke confirmation is open
    // When it is confirmed
    // Then the key is revoked
    it("revokes the key after confirmation", async () => {
        const { user } = renderPage();
        await screen.findByText("Production");

        await user.click(screen.getByRole("button", { name: "Revoke" }));
        await user.click(await screen.findByRole("button", { name: "Revoke key" }));

        await waitFor(() =>
            expect(del).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/api-keys/key-active`),
        );
    });

    // Given the revoke confirmation is open
    // When it is cancelled
    // Then the key stays active
    it("does not revoke when the confirmation is cancelled", async () => {
        const { user } = renderPage();
        await screen.findByText("Production");

        await user.click(screen.getByRole("button", { name: "Revoke" }));
        await user.click(await screen.findByRole("button", { name: "Cancel" }));

        expect(del).not.toHaveBeenCalled();
    });
});
