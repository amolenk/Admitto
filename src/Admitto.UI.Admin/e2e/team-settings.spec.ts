import { expect, test } from "@playwright/test";
import { resolveDemoTeam } from "./support/demo-data";
import { gotoTeamSettings } from "./support/navigation";

test("renders the configured team name in the server-rendered settings heading", async ({ page }) => {
    const team = await resolveDemoTeam(page);

    await gotoTeamSettings(page, team.teamId);

    const settingsHeading = page.locator("h1:visible").first();
    await expect(settingsHeading).toHaveText(team.name);
    await expect(settingsHeading).not.toHaveText(team.teamId);
});
