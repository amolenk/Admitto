import { describe, expect, it } from "vitest";

import { redirectMock } from "@/test-utils/router";

import EventEmailsPage from "./page";

// Harvested from openspec `admin-ui-bulk-emails` ("Navigating to /emails shows Campaigns tab
// by default" / "Old emails URL redirects to campaigns tab"). This is a server component with
// no markup, so it is exercised by calling it directly rather than rendering it.

describe("event emails index page", () => {
    // Given the bare /emails path, which has no content of its own
    // When it is requested
    // Then the organizer is sent to the Campaigns tab, the default email view
    it("redirects to the campaigns tab", async () => {
        await EventEmailsPage({
            params: Promise.resolve({ teamId: "acme", eventId: "devconf-2026" }),
        });

        expect(redirectMock).toHaveBeenCalledWith("/teams/acme/events/devconf-2026/emails/campaigns");
    });
});
