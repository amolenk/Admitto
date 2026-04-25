"use client";

import * as React from "react";
import { format } from "date-fns";
import { Calendar as CalendarIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import {
    formatZoneCaption,
    utcIsoToWallClock,
    wallClockToUtcIso,
} from "@/lib/time-zones";

// Time-zone-aware drop-in replacement for the plain DateTimePicker.
//
// `value` is a UTC ISO string (e.g. "2026-06-01T16:00:00.000Z") or an empty
// string. The picker shows the corresponding wall-clock time in `timeZone`
// and emits a UTC ISO string back through `onChange`. This way callers can
// pass `event.startsAt` straight in and submit the field value straight out
// without any timezone conversion at the call site.

const WALL_CLOCK = "yyyy-MM-dd'T'HH:mm";
const TIME_PART = "HH:mm";

export interface ZonedDateTimePickerProps {
    value?: string;
    onChange?: (utcIso: string) => void;
    onBlur?: () => void;
    disabled?: boolean;
    placeholder?: string;
    className?: string;
    name?: string;
    timeZone: string;
    showZoneCaption?: boolean;
    defaultTime?: string; // HH:mm used when picking a date for the first time
}

export const ZonedDateTimePicker = React.forwardRef<HTMLButtonElement, ZonedDateTimePickerProps>(
    function ZonedDateTimePicker(
        {
            value,
            onChange,
            onBlur,
            disabled,
            placeholder = "Pick a date",
            className,
            name,
            timeZone,
            showZoneCaption = true,
            defaultTime = "09:00",
        },
        ref,
    ) {
        // Wall-clock view of `value` in the event's zone.
        const wallClock = utcIsoToWallClock(value, timeZone);
        const wallDate = wallClock ? wallClock.slice(0, 10) : "";
        const wallTime = wallClock ? wallClock.slice(11, 16) : "";

        // Build a calendar-friendly Date that represents the same wall-clock
        // moment in the browser's zone (so the calendar grid highlights it).
        const calendarDate = wallDate
            ? new Date(`${wallDate}T${wallTime || "00:00"}:00`)
            : undefined;

        function emit(nextDate: string, nextTime: string) {
            if (!nextDate) {
                onChange?.("");
                return;
            }
            const time = nextTime || defaultTime;
            const wall = `${nextDate}T${time}`;
            onChange?.(wallClockToUtcIso(wall, timeZone));
        }

        function handleDate(next: Date | undefined) {
            if (!next) {
                emit("", "");
                return;
            }
            emit(format(next, "yyyy-MM-dd"), wallTime);
        }

        function handleTime(next: string) {
            emit(wallDate, next);
        }

        const caption = showZoneCaption ? formatZoneCaption(timeZone) : "";

        return (
            <div className="space-y-1">
                <Popover onOpenChange={(open) => { if (!open) onBlur?.(); }}>
                    <PopoverTrigger asChild>
                        <Button
                            ref={ref}
                            type="button"
                            variant="outline"
                            disabled={disabled}
                            name={name}
                            className={cn(
                                "w-[260px] justify-start text-left font-normal",
                                !wallDate && "text-muted-foreground",
                                className,
                            )}
                        >
                            <CalendarIcon className="size-4" />
                            {wallDate
                                ? `${format(new Date(`${wallDate}T00:00:00`), "LLL dd, y")} \u00b7 ${wallTime || "--:--"}`
                                : <span>{placeholder}</span>}
                        </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-auto p-0" align="start">
                        <Calendar
                            mode="single"
                            selected={calendarDate}
                            onSelect={handleDate}
                            initialFocus
                            defaultMonth={calendarDate}
                        />
                        <div className="border-t p-3 flex items-center gap-2">
                            <label className="text-[12px] text-muted-foreground w-12">Time</label>
                            <Input
                                type="time"
                                value={wallTime}
                                onChange={(e) => handleTime(e.target.value)}
                                disabled={disabled || !wallDate}
                                className="w-[120px]"
                                step={60}
                            />
                        </div>
                    </PopoverContent>
                </Popover>
                {caption && (
                    <p className="text-[11px] text-muted-foreground">{caption}</p>
                )}
            </div>
        );
    },
);

// Convenience wrapper that mirrors the WALL_CLOCK pattern used by callers
// that still hold raw wall-clock strings rather than UTC ISO values. Kept as
// a re-export so callers don't need to know which library produced it.
export const ZONED_WALL_CLOCK_FORMAT = WALL_CLOCK;
