export type EventLifecycleStatus = "active" | "archived";

export interface RegistrationPolicy {
    opensAt: string;
    closesAt: string;
    allowedEmailDomain: string | null;
}

export interface ReconfirmPolicy {
    opensAt: string;
    closesAt: string;
    minEmailIntervalHours: number;
    quietHoursStart: string | null;
    quietHoursEnd: string | null;
}

export interface WaitlistPolicy {
    quietHoursStart: string;
    quietHoursEnd: string;
}

export interface AdditionalDetailField {
    key: string;
    name: string;
    maxLength: number;
}

export interface TicketedEventDetails {
    id: string;
    teamId: string;
    slug: string;
    name: string;
    startsAt: string;
    endsAt: string;
    timeZone: string;
    status: EventLifecycleStatus | string;
    version: number | string;
    isRegistrationOpen: boolean;
    registrationPolicy: RegistrationPolicy | null;
    reconfirmPolicy: ReconfirmPolicy | null;
    waitlistPolicy: WaitlistPolicy;
    additionalDetailSchema?: AdditionalDetailField[];
    websiteUrl?: string;
    baseUrl?: string;
}

export function normalizeStatus(status: string): EventLifecycleStatus {
    return status.toLowerCase() as EventLifecycleStatus;
}

export function isEventActive(status: string): boolean {
    return normalizeStatus(status) === "active";
}
