import { mkdir } from "node:fs/promises";
import { dirname } from "node:path";
import { expect, test as setup } from "@playwright/test";
import {
    appUrl,
    demoTeamName,
    requireE2eBaseUrl,
    requireE2eCredentials,
} from "./support/runtime";

const authFile = "playwright/.auth/alice.json";

function escapeRegExp(value: string): string {
    return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

setup("authenticate Alice through Keycloak", async ({ page }) => {
    const { username, password } = requireE2eCredentials();
    const baseUrl = requireE2eBaseUrl();

    await page.goto(appUrl("/signin"));

    // The local Keycloak realm presents its standard username/password form.
    // Keep these locators semantic so a changed IdP theme fails visibly here.
    await page.getByRole("textbox", { name: /username|email/i }).fill(username);
    await page.getByRole("textbox", { name: /password/i }).fill(password);
    await page.getByRole("button", { name: /sign in|log in|login/i }).click();

    await expect(page).toHaveURL(
        new RegExp(`^${escapeRegExp(baseUrl.replace(/\/$/, ""))}(?:/|/teams(?:/|$))`),
    );
    await expect(
        page.getByRole("button", {
            name: new RegExp(`(?:No teams found|${escapeRegExp(demoTeamName())})`),
        }),
    ).toBeVisible();

    await mkdir(dirname(authFile), { recursive: true });
    await page.context().storageState({ path: authFile });
});
