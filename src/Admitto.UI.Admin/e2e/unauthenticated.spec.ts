import { expect, test } from "@playwright/test";
import { appUrl, demoTeamName } from "./support/runtime";

test("redirects an unauthenticated team settings request into the sign-in flow", async ({ page }) => {
    const safeTeamRoute = encodeURIComponent(demoTeamName());

    await page.goto(appUrl(`/teams/${safeTeamRoute}/settings`));

    await expect(page).toHaveURL(/\/protocol\/openid-connect\/auth(?:\?|$)/);
    await expect(page.getByRole("textbox", { name: /username|email/i })).toBeVisible();
});
