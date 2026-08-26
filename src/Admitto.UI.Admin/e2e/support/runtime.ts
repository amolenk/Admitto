const DEFAULT_DEMO_TEAM_NAME = "Admitto Demo";
const DEFAULT_DEMO_EVENT_NAME = "Admitto Demo Event";

function requiredEnvironmentValue(name: string): string {
    const value = process.env[name]?.trim();
    if (!value) {
        throw new Error(`${name} is required to execute this Playwright operation.`);
    }

    return value;
}

export function requireE2eBaseUrl(): string {
    return requiredEnvironmentValue("E2E_BASE_URL");
}

export function requireE2eCredentials(): { username: string; password: string } {
    return {
        username: requiredEnvironmentValue("E2E_USERNAME"),
        password: requiredEnvironmentValue("E2E_PASSWORD"),
    };
}

export function demoTeamName(): string {
    return process.env.E2E_DEMO_TEAM_NAME?.trim() || DEFAULT_DEMO_TEAM_NAME;
}

export function demoEventName(): string {
    return process.env.E2E_DEMO_EVENT_NAME?.trim() || DEFAULT_DEMO_EVENT_NAME;
}

export function appUrl(pathname: string): string {
    return new URL(pathname, requireE2eBaseUrl()).toString();
}

export const demoDefaults = {
    teamName: DEFAULT_DEMO_TEAM_NAME,
    eventName: DEFAULT_DEMO_EVENT_NAME,
} as const;
