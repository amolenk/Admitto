import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { ticketedEventDetailsDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import EditEventLayout from "./layout";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const TEAM_ID = "44444444-4444-4444-4444-444444444444";
const EVENT_ID = "55555555-5555-5555-5555-555555555555";

describe("EditEventLayout", () => {
    beforeEach(() => {
        get.mockResolvedValue(
            ticketedEventDetailsDto({ id: EVENT_ID, teamId: TEAM_ID, name: "Acme Summit" }),
        );
    });

    it("provides a Policies tab link for the event", async () => {
        setRoute({
            params: { teamId: TEAM_ID, eventId: EVENT_ID },
            pathname: `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/general`,
        });

        renderWithProviders(
            <EditEventLayout>
                <div>General settings</div>
            </EditEventLayout>,
        );

        const policiesLink = await screen.findByRole("link", { name: "Policies" });
        expect(policiesLink).toHaveAttribute(
            "href",
            `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/policies`,
        );
        expect(screen.getByRole("link", { name: "General" })).toHaveAttribute(
            "href",
            `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/general`,
        );
        expect(screen.getByRole("link", { name: "Danger zone" })).toHaveAttribute(
            "href",
            `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/danger`,
        );
    });

    it.each([
        ["Policies", "policies"],
        ["Danger zone", "danger"],
    ])("marks the %s tab active on its route", async (label, segment) => {
        setRoute({
            params: { teamId: TEAM_ID, eventId: EVENT_ID },
            pathname: `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/${segment}`,
        });

        renderWithProviders(
            <EditEventLayout>
                <div>{label} settings</div>
            </EditEventLayout>,
        );

        const link = await screen.findByRole("link", { name: label });
        expect(link.className).toContain("border-primary");
        expect(screen.getByRole("link", { name: "General" }).className).not.toContain(
            "border-primary",
        );
    });
});
