// Helpers for working with IANA time zones in admin UI date pickers and
// read-only event datetime displays.

import { formatInTimeZone, fromZonedTime } from "date-fns-tz";

// A small curated list of common IANA zones used as suggestions. The selector
// also accepts free-text so any zone supported by Intl.DateTimeFormat works.
export const COMMON_TIME_ZONES: readonly string[] = [
    "UTC",
    "Europe/Amsterdam",
    "Europe/Berlin",
    "Europe/Brussels",
    "Europe/Dublin",
    "Europe/Lisbon",
    "Europe/London",
    "Europe/Madrid",
    "Europe/Paris",
    "Europe/Prague",
    "Europe/Rome",
    "Europe/Stockholm",
    "Europe/Warsaw",
    "Europe/Zurich",
    "America/New_York",
    "America/Chicago",
    "America/Denver",
    "America/Los_Angeles",
    "America/Toronto",
    "America/Sao_Paulo",
    "America/Mexico_City",
    "Asia/Tokyo",
    "Asia/Singapore",
    "Asia/Hong_Kong",
    "Asia/Shanghai",
    "Asia/Dubai",
    "Asia/Kolkata",
    "Australia/Sydney",
    "Australia/Melbourne",
    "Pacific/Auckland",
];

export function detectBrowserTimeZone(): string {
    try {
        return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
    } catch {
        return "UTC";
    }
}

// Returns true when the IANA zone is recognised by the browser's Intl API.
export function isValidTimeZone(zone: string): boolean {
    if (!zone) return false;
    try {
        // Throws RangeError for unknown zones.
        new Intl.DateTimeFormat("en-US", { timeZone: zone });
        return true;
    } catch {
        return false;
    }
}

// "Europe/Amsterdam (UTC+02:00)" — used as a caption beside pickers and
// read-only datetime displays so the active zone is never ambiguous.
export function formatZoneCaption(zone: string, at: Date = new Date()): string {
    if (!zone) return "";
    try {
        const offset = formatInTimeZone(at, zone, "XXX"); // e.g. +02:00 or Z
        const utcOffset = offset === "Z" ? "UTC" : `UTC${offset}`;
        return `${zone} (${utcOffset})`;
    } catch {
        return zone;
    }
}

// Format a stored UTC instant in the event's zone for read-only displays.
export function formatInEventZone(
    iso: string | Date | null | undefined,
    zone: string,
    pattern: string,
): string {
    if (!iso) return "";
    const date = iso instanceof Date ? iso : new Date(iso);
    if (Number.isNaN(date.getTime())) return "";
    try {
        return formatInTimeZone(date, zone, pattern);
    } catch {
        return formatInTimeZone(date, "UTC", pattern);
    }
}

// Turn a wall-clock `YYYY-MM-DDTHH:mm` (interpreted in `zone`) into a UTC ISO
// string. Used by date-time pickers that bind to the event's zone.
export function wallClockToUtcIso(wallClock: string, zone: string): string {
    if (!wallClock) return "";
    try {
        return fromZonedTime(wallClock, zone).toISOString();
    } catch {
        return "";
    }
}

// Turn a UTC ISO string into the local `YYYY-MM-DDTHH:mm` representation in
// `zone`. The reverse of `wallClockToUtcIso`.
export function utcIsoToWallClock(iso: string | null | undefined, zone: string): string {
    if (!iso) return "";
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return "";
    try {
        return formatInTimeZone(date, zone, "yyyy-MM-dd'T'HH:mm");
    } catch {
        return "";
    }
}
