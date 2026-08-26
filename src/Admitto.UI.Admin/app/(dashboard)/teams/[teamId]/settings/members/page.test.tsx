import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { teamMemberDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import MembersPage from "./page";

// An invalid email disables the Add button rather than showing a field-level validation error.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);
const put = vi.mocked(apiClient.put);
const del = vi.mocked(apiClient.delete);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const alice = teamMemberDto("alice@example.com", "crew");
const bob = teamMemberDto("bob@example.com", "owner");

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID } });
    return renderWithProviders(<MembersPage />);
}

/** The row for a member, so role and remove controls can be scoped to one member. */
function memberRow(email: string) {
    return screen.getByText(email).closest("div.flex.items-center") as HTMLElement;
}

/** Picks an option from a Radix Select trigger. */
async function selectOption(
    user: ReturnType<typeof renderPage>["user"],
    trigger: HTMLElement,
    optionName: string,
) {
    await user.click(trigger);
    await user.click(await screen.findByRole("option", { name: optionName }));
}

describe("MembersPage list", () => {
    beforeEach(() => {
        get.mockResolvedValue([alice, bob]);
    });

    // Given a team with two members
    // When the page loads
    // Then each member is listed with their current role
    it("lists members with their roles", async () => {
        renderPage();

        expect(await screen.findByText("alice@example.com")).toBeInTheDocument();
        expect(within(memberRow("alice@example.com")).getByRole("combobox")).toHaveTextContent(
            "Crew",
        );
        expect(within(memberRow("bob@example.com")).getByRole("combobox")).toHaveTextContent(
            "Owner",
        );
    });

    // Given a team with no members
    // When the page loads
    // Then an empty state is shown rather than a bare table
    it("shows an empty state when the team has no members", async () => {
        get.mockResolvedValue([]);

        renderPage();

        expect(await screen.findByText("No members yet")).toBeInTheDocument();
    });
});

describe("MembersPage add member", () => {
    beforeEach(() => {
        get.mockResolvedValue([alice, bob]);
        post.mockResolvedValue(undefined);
    });

    // Given a valid email and the default Crew role
    // When the owner adds the member
    // Then the member is posted and the form is cleared, ready for the next entry
    it("adds a member and clears the form", async () => {
        const { user } = renderPage();
        const email = screen.getByPlaceholderText("member@example.com");

        await user.type(email, "charlie@example.com");
        await user.click(screen.getByRole("button", { name: /add member/i }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/members`, {
                email: "charlie@example.com",
                role: "crew",
            }),
        );
        await waitFor(() => expect(email).toHaveValue(""));
    });

    // Given a role other than the default is chosen
    // When the member is added
    // Then that role is sent
    it("sends the chosen role", async () => {
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("member@example.com"), "charlie@example.com");
        await selectOption(user, screen.getAllByRole("combobox")[0]!, "Organizer");
        await user.click(screen.getByRole("button", { name: /add member/i }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/members`,
                expect.objectContaining({ role: "organizer" }),
            ),
        );
    });

    // Given a malformed email address
    // When the owner looks at the form
    // Then the Add button is disabled, so nothing reaches the backend
    it("disables the add button until the email is well-formed", async () => {
        const { user } = renderPage();
        const add = screen.getByRole("button", { name: /add member/i });

        expect(add).toBeDisabled();

        await user.type(screen.getByPlaceholderText("member@example.com"), "not-an-email");
        expect(add).toBeDisabled();

        await user.type(screen.getByPlaceholderText("member@example.com"), "@example.com");
        expect(add).toBeEnabled();

        expect(post).not.toHaveBeenCalled();
    });

    // Given the person is already a member
    // When the backend rejects the addition
    // Then the reason is shown
    it("shows the backend's reason when the member already exists", async () => {
        post.mockRejectedValue(
            new FormError({ status: 409, title: "Conflict", detail: "User is already a member." }),
        );
        const { user } = renderPage();

        await user.type(screen.getByPlaceholderText("member@example.com"), "alice@example.com");
        await user.click(screen.getByRole("button", { name: /add member/i }));

        expect(await screen.findByText("User is already a member.")).toBeInTheDocument();
    });
});

describe("MembersPage change role", () => {
    beforeEach(() => {
        get.mockResolvedValue([alice, bob]);
        put.mockResolvedValue(undefined);
    });

    // Given a Crew member
    // When the owner picks Organizer from the row's dropdown
    // Then the change is sent immediately, without a separate save step
    it("changes a member's role on selection", async () => {
        const { user } = renderPage();
        await screen.findByText("alice@example.com");

        await selectOption(
            user,
            within(memberRow("alice@example.com")).getByRole("combobox"),
            "Organizer",
        );

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/members/alice%40example.com`,
                { newRole: "organizer" },
            ),
        );
    });

    // Given the backend rejects the role change
    // When it fails
    // Then the error is shown and the displayed role stays at the stored value, because the
    // dropdown is driven by server data rather than optimistic local state
    it("shows an error and keeps the stored role when the change fails", async () => {
        put.mockRejectedValue(
            new FormError({ status: 403, title: "Forbidden", detail: "Cannot demote the last owner." }),
        );
        const { user } = renderPage();
        await screen.findByText("alice@example.com");

        await selectOption(
            user,
            within(memberRow("alice@example.com")).getByRole("combobox"),
            "Organizer",
        );

        expect(await screen.findByText("Cannot demote the last owner.")).toBeInTheDocument();
        expect(within(memberRow("alice@example.com")).getByRole("combobox")).toHaveTextContent(
            "Crew",
        );
    });
});

describe("MembersPage remove member", () => {
    beforeEach(() => {
        get.mockResolvedValue([alice, bob]);
        del.mockResolvedValue(undefined);
    });

    // Given the owner opened the remove confirmation for a member
    // When they confirm
    // Then that member — and only that member — is removed
    it("removes the member after confirmation", async () => {
        const { user } = renderPage();
        await screen.findByText("alice@example.com");

        await user.click(within(memberRow("alice@example.com")).getByRole("button"));
        await user.click(await screen.findByRole("button", { name: "Remove" }));

        await waitFor(() =>
            expect(del).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/members/alice%40example.com`),
        );
        expect(del).toHaveBeenCalledTimes(1);
    });

    // Given the remove confirmation is open
    // When the owner cancels
    // Then nothing is removed
    it("does not remove the member when the confirmation is cancelled", async () => {
        const { user } = renderPage();
        await screen.findByText("alice@example.com");

        await user.click(within(memberRow("alice@example.com")).getByRole("button"));
        await user.click(await screen.findByRole("button", { name: "Cancel" }));

        expect(del).not.toHaveBeenCalled();
    });
});
