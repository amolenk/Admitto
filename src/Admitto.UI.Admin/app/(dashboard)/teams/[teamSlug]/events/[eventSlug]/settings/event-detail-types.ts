export type EventLifecycleStatus = "active" | "cancelled" | "archived";

export interface RegistrationPolicy {
    opensAt: string;
    closesAt: string;
    allowedEmailDomain: string | null;
}

export interface CancellationPolicy {
    lateCancellationCutoff: string;
}

export interface ReconfirmPolicy {
    opensAt: string;
    closesAt: string;
    cadenceDays: number;
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
    cancellationPolicy: CancellationPolicy | null;
    reconfirmPolicy: ReconfirmPolicy | null;
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
