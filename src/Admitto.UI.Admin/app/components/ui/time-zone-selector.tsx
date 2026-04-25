"use client";

import * as React from "react";
import { Check, ChevronsUpDown, Globe2 } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
} from "@/components/ui/command";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import {
    COMMON_TIME_ZONES,
    formatZoneCaption,
    isValidTimeZone,
} from "@/lib/time-zones";

// Searchable IANA time-zone selector. Suggests common zones up front and
// accepts free-text for less common ones — the typed string is committed as
// the value provided it round-trips through `Intl.DateTimeFormat`.

export interface TimeZoneSelectorProps {
    value?: string;
    onChange?: (zone: string) => void;
    onBlur?: () => void;
    disabled?: boolean;
    className?: string;
    placeholder?: string;
}

export const TimeZoneSelector = React.forwardRef<HTMLButtonElement, TimeZoneSelectorProps>(
    function TimeZoneSelector(
        { value, onChange, onBlur, disabled, className, placeholder = "Select time zone" },
        ref,
    ) {
        const [open, setOpen] = React.useState(false);
        const [query, setQuery] = React.useState("");

        const trimmedQuery = query.trim();
        const showFreeText =
            trimmedQuery.length > 0 &&
            !COMMON_TIME_ZONES.some((z) => z.toLowerCase() === trimmedQuery.toLowerCase()) &&
            isValidTimeZone(trimmedQuery);

        function commit(zone: string) {
            onChange?.(zone);
            setOpen(false);
            setQuery("");
        }

        const caption = value ? formatZoneCaption(value) : "";

        return (
            <div className="space-y-1">
                <Popover
                    open={open}
                    onOpenChange={(next) => {
                        setOpen(next);
                        if (!next) onBlur?.();
                    }}
                >
                    <PopoverTrigger asChild>
                        <Button
                            ref={ref}
                            type="button"
                            variant="outline"
                            role="combobox"
                            aria-expanded={open}
                            disabled={disabled}
                            className={cn(
                                "w-[320px] justify-between font-normal",
                                !value && "text-muted-foreground",
                                className,
                            )}
                        >
                            <span className="flex items-center gap-2 truncate">
                                <Globe2 className="size-4 shrink-0" />
                                <span className="truncate">{value || placeholder}</span>
                            </span>
                            <ChevronsUpDown className="size-4 opacity-50" />
                        </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-[320px] p-0" align="start">
                        <Command shouldFilter={true}>
                            <CommandInput
                                placeholder="Search IANA zone\u2026"
                                value={query}
                                onValueChange={setQuery}
                            />
                            <CommandList>
                                <CommandEmpty>
                                    {showFreeText
                                        ? "Press the suggestion below to use this zone."
                                        : "No matching zone."}
                                </CommandEmpty>
                                {showFreeText && (
                                    <CommandGroup heading="Custom">
                                        <CommandItem
                                            value={trimmedQuery}
                                            onSelect={() => commit(trimmedQuery)}
                                        >
                                            <Check className="mr-2 size-4 opacity-0" />
                                            Use &ldquo;{trimmedQuery}&rdquo;
                                        </CommandItem>
                                    </CommandGroup>
                                )}
                                <CommandGroup heading="Common zones">
                                    {COMMON_TIME_ZONES.map((zone) => (
                                        <CommandItem
                                            key={zone}
                                            value={zone}
                                            onSelect={() => commit(zone)}
                                        >
                                            <Check
                                                className={cn(
                                                    "mr-2 size-4",
                                                    value === zone ? "opacity-100" : "opacity-0",
                                                )}
                                            />
                                            {zone}
                                        </CommandItem>
                                    ))}
                                </CommandGroup>
                            </CommandList>
                        </Command>
                    </PopoverContent>
                </Popover>
                {caption && (
                    <p className="text-[11px] text-muted-foreground">{caption}</p>
                )}
            </div>
        );
    },
);
