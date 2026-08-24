import { describe, expect, it } from "vitest";

import {
    formatInEventZone,
    formatZoneCaption,
    isValidTimeZone,
    utcIsoToWallClock,
    wallClockToUtcIso,
} from "./time-zones";

// Europe/Amsterdam is UTC+1 (CET) in winter and UTC+2 (CEST) in summer; in 2026 the
// switch is on 29 March. Dates either side of it are used throughout to make sure the
// offset is derived from the instant rather than assumed.

describe("wallClockToUtcIso", () => {
    // Given a wall-clock time in winter, before the CET -> CEST switch
    // When converting it to a UTC instant
    // Then the UTC+1 offset is applied
    it("applies the winter offset for a date before the DST switch", () => {
        expect(wallClockToUtcIso("2026-03-01T12:00", "Europe/Amsterdam")).toBe(
            "2026-03-01T11:00:00.000Z",
        );
    });

    // Given a wall-clock time in summer, after the CET -> CEST switch
    // When converting it to a UTC instant
    // Then the UTC+2 offset is applied
    it("applies the summer offset for a date after the DST switch", () => {
        expect(wallClockToUtcIso("2026-06-01T12:00", "Europe/Amsterdam")).toBe(
            "2026-06-01T10:00:00.000Z",
        );
    });

    // Given an empty wall-clock string
    // When converting it
    // Then an empty string is returned rather than an invalid date
    it("returns an empty string for empty input", () => {
        expect(wallClockToUtcIso("", "Europe/Amsterdam")).toBe("");
    });
});

describe("utcIsoToWallClock", () => {
    // Given a UTC instant in summer
    // When rendering it as a wall clock in the event zone
    // Then the local time is shown, not the UTC time
    it("renders the local wall clock for the zone", () => {
        expect(utcIsoToWallClock("2026-06-01T10:00:00.000Z", "Europe/Amsterdam")).toBe(
            "2026-06-01T12:00",
        );
    });

    // Given a null or unparseable instant
    // When rendering it
    // Then an empty string is returned
    it.each([
        ["null", null],
        ["undefined", undefined],
        ["unparseable", "not-a-date"],
    ])("returns an empty string for %s input", (_label, input) => {
        expect(utcIsoToWallClock(input as string | null | undefined, "Europe/Amsterdam")).toBe("");
    });
});

describe("wall-clock round trip", () => {
    // Given wall-clock times either side of the DST boundary
    // When each is converted to UTC and back again
    // Then the original wall-clock string is recovered
    it.each([
        ["winter", "2026-03-01T12:00"],
        ["summer", "2026-06-01T12:00"],
        ["day before the switch", "2026-03-28T23:30"],
        ["day after the switch", "2026-03-30T00:30"],
    ])("round-trips a %s wall clock unchanged", (_label, wallClock) => {
        const utc = wallClockToUtcIso(wallClock, "Europe/Amsterdam");

        expect(utcIsoToWallClock(utc, "Europe/Amsterdam")).toBe(wallClock);
    });
});

describe("isValidTimeZone", () => {
    // Given an IANA zone the Intl API recognises
    // When validating it
    // Then it is accepted
    it.each(["UTC", "Europe/Amsterdam", "America/New_York"])("accepts %s", (zone) => {
        expect(isValidTimeZone(zone)).toBe(true);
    });

    // Given an empty or unrecognised zone
    // When validating it
    // Then it is rejected instead of throwing
    it.each(["", "Not/AZone", "Europe/Nowhere"])("rejects %s", (zone) => {
        expect(isValidTimeZone(zone)).toBe(false);
    });
});

describe("formatZoneCaption", () => {
    // Given a zone with a non-zero offset at the given instant
    // When building the caption
    // Then the zone and its UTC offset are shown
    it("shows the offset that applies at the given instant", () => {
        const winter = new Date("2026-03-01T11:00:00Z");
        const summer = new Date("2026-06-01T10:00:00Z");

        expect(formatZoneCaption("Europe/Amsterdam", winter)).toBe("Europe/Amsterdam (UTC+01:00)");
        expect(formatZoneCaption("Europe/Amsterdam", summer)).toBe("Europe/Amsterdam (UTC+02:00)");
    });

    // Given UTC, whose ISO offset renders as "Z" rather than "+00:00"
    // When building the caption
    // Then "Z" is translated to the literal "UTC"
    it("renders a zero offset as UTC rather than Z", () => {
        expect(formatZoneCaption("UTC", new Date("2026-06-01T10:00:00Z"))).toBe("UTC (UTC)");
    });

    // Given an empty zone
    // When building the caption
    // Then an empty string is returned so no stray parentheses are rendered
    it("returns an empty string for an empty zone", () => {
        expect(formatZoneCaption("")).toBe("");
    });
});

describe("formatInEventZone", () => {
    // Given a stored UTC instant and the event's zone
    // When formatting it for a read-only display
    // Then it is rendered in the event zone
    it("formats the instant in the event zone", () => {
        expect(formatInEventZone("2026-06-01T10:00:00Z", "Europe/Amsterdam", "yyyy-MM-dd HH:mm")).toBe(
            "2026-06-01 12:00",
        );
    });

    // Given an unrecognised zone
    // When formatting an instant
    // Then it silently falls back to UTC rather than throwing
    it("falls back to UTC for an unrecognised zone", () => {
        expect(formatInEventZone("2026-06-01T10:00:00Z", "Not/AZone", "yyyy-MM-dd HH:mm")).toBe(
            "2026-06-01 10:00",
        );
    });

    // Given a missing or unparseable instant
    // When formatting it
    // Then an empty string is returned
    it.each([
        ["null", null],
        ["empty", ""],
        ["unparseable", "not-a-date"],
    ])("returns an empty string for %s input", (_label, input) => {
        expect(formatInEventZone(input, "Europe/Amsterdam", "yyyy-MM-dd")).toBe("");
    });
});
