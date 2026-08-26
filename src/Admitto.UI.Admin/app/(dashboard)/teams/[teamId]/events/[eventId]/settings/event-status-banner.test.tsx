import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { EventStatusBanner } from "./event-status-banner";
import { renderWithProviders } from "@/test-utils/render";

describe("EventStatusBanner", () => {
    it("shows the archived status and read-only policy message", () => {
        renderWithProviders(<EventStatusBanner status="archived" />);

        expect(screen.getByText("Event archived")).toBeInTheDocument();
        expect(
            screen.getByText("This event has been archived. Policies are read-only and cannot be modified."),
        ).toBeInTheDocument();
    });
});
