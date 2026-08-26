import { expect, type Page } from "@playwright/test";
import { appUrl, demoEventName } from "./runtime";

export type EventSettingsSection = "general" | "policies" | "danger";

export async function gotoTeamSettings(page: Page, teamId: string): Promise<void> {
    const url = appUrl(`/teams/${teamId}/settings`);
    await page.goto(url);
    await expect(page).toHaveURL(url);
    await expect(page.getByRole("heading", { name: "General" })).toBeVisible();
}

export async function gotoEventSettings(
    page: Page,
    teamId: string,
    eventId: string,
    section: EventSettingsSection = "general",
): Promise<void> {
    const url = appUrl(`/teams/${teamId}/events/${eventId}/edit/${section}`);
    await page.goto(url);
    await expect(page).toHaveURL(url);
}

export async function gotoTicketTypes(page: Page, teamId: string, eventId: string): Promise<void> {
    const url = appUrl(`/teams/${teamId}/events/${eventId}/ticket-types`);
    await page.goto(url);
    await expect(page).toHaveURL(url);
    await expect(page.getByRole("heading", { name: demoEventName() })).toBeVisible();
}
