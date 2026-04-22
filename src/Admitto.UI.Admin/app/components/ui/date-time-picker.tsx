"use client";

import * as React from "react";
import { format, parse } from "date-fns";
import { Calendar as CalendarIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Input } from "@/components/ui/input";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

// Drop-in replacement for `<input type="datetime-local">`.
// Accepts and emits values in the same `YYYY-MM-DDTHH:mm` string format.

const DATETIME_LOCAL = "yyyy-MM-dd'T'HH:mm";
const DATE_PART = "yyyy-MM-dd";
const TIME_PART = "HH:mm";

function parseValue(value: string | undefined): { date: Date | undefined; time: string } {
    if (!value) return { date: undefined, time: "" };
    const parsed = parse(value, DATETIME_LOCAL, new Date());
    if (Number.isNaN(parsed.getTime())) return { date: undefined, time: "" };
    return { date: parsed, time: format(parsed, TIME_PART) };
}

function combine(date: Date | undefined, time: string): string {
    if (!date) return "";
    const [hh, mm] = (time || "00:00").split(":");
    const d = new Date(date);
    d.setHours(Number(hh) || 0, Number(mm) || 0, 0, 0);
    return format(d, DATETIME_LOCAL);
}

export interface DateTimePickerProps {
    value?: string;
    onChange?: (value: string) => void;
    onBlur?: () => void;
    disabled?: boolean;
    placeholder?: string;
    className?: string;
    name?: string;
}

export const DateTimePicker = React.forwardRef<HTMLButtonElement, DateTimePickerProps>(
    function DateTimePicker(
        { value, onChange, onBlur, disabled, placeholder = "Pick a date", className, name },
        ref,
    ) {
        const { date, time } = parseValue(value);

        function handleDate(next: Date | undefined) {
            onChange?.(combine(next, time || "09:00"));
        }

        function handleTime(next: string) {
            onChange?.(combine(date, next));
        }

        return (
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
                            !date && "text-muted-foreground",
                            className,
                        )}
                    >
                        <CalendarIcon className="size-4" />
                        {date
                            ? `${format(date, "LLL dd, y")} \u00b7 ${time || "--:--"}`
                            : <span>{placeholder}</span>}
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                        mode="single"
                        selected={date}
                        onSelect={handleDate}
                        initialFocus
                        defaultMonth={date}
                    />
                    <div className="border-t p-3 flex items-center gap-2">
                        <label className="text-[12px] text-muted-foreground w-12">Time</label>
                        <Input
                            type="time"
                            value={time}
                            onChange={(e) => handleTime(e.target.value)}
                            disabled={disabled || !date}
                            className="w-[120px]"
                            step={60}
                        />
                    </div>
                </PopoverContent>
            </Popover>
        );
    },
);

// Re-export a helper for callers that want to format a stored ISO value
// into the local `YYYY-MM-DDTHH:mm` shape this picker expects.
export function toDateTimeLocal(value: string | Date | null | undefined): string {
    if (!value) return "";
    const d = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(d.getTime())) return "";
    return format(d, DATETIME_LOCAL);
}
