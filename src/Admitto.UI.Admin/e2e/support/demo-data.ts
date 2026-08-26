import { expect, type Page } from "@playwright/test";
import { appUrl, demoEventName, demoTeamName } from "./runtime";

export type DemoTeam = {
    teamId: string;
    name: string;
    canManageTeamSettings: boolean;
    canCreateEvents: boolean;
};

export type DemoEvent = {
    id: string;
    teamId: string;
    name: string;
    status: "active" | "archived";
};

export type DemoContext = {
    team: DemoTeam;
    event: DemoEvent;
};

const seedWaitOptions = {
    timeout: 120_000,
    intervals: [100, 250, 500, 1_000, 2_000, 5_000],
};

async function getJson<T>(page: Page, pathname: string): Promise<T | undefined> {
    const response = await page.context().request.get(appUrl(pathname));
    if (!response.ok()) {
        return undefined;
    }

    return (await response.json()) as T;
}

export async function resolveDemoTeam(page: Page, name = demoTeamName()): Promise<DemoTeam> {
    let resolved: DemoTeam | undefined;

    await expect
        .poll(
            async () => {
                const teams = await getJson<DemoTeam[]>(page, "/api/teams");
                resolved = teams?.find((team) => team.name === name);
                return resolved?.teamId ?? null;
            },
            { ...seedWaitOptions, message: `Waiting for demo team ${name}` },
        )
        .not.toBeNull();

    return resolved!;
}

export async function resolveDemoEvent(
    page: Page,
    teamId: string,
    name = demoEventName(),
): Promise<DemoEvent> {
    let resolved: DemoEvent | undefined;

    await expect
        .poll(
            async () => {
                const events = await getJson<Array<Omit<DemoEvent, "teamId">>>(
                    page,
                    `/api/teams/${teamId}/events`,
                );
                const event = events?.find(
                    (candidate) => candidate.status !== "archived" && candidate.name === name,
                );
                resolved = event ? { ...event, teamId } : undefined;
                return resolved?.id ?? null;
            },
            { ...seedWaitOptions, message: `Waiting for demo event ${name}` },
        )
        .not.toBeNull();

    return resolved!;
}

export async function resolveDemoContext(page: Page): Promise<DemoContext> {
    const team = await resolveDemoTeam(page);
    const event = await resolveDemoEvent(page, team.teamId);
    return { team, event };
}
