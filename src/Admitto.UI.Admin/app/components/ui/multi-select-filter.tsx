"use client";

import { ChevronDown } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
    DropdownMenu,
    DropdownMenuCheckboxItem,
    DropdownMenuContent,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";

export interface MultiSelectFilterOption<T extends string> {
    value: T;
    label: string;
}

interface MultiSelectFilterProps<T extends string> {
    /** Accessible name for the trigger button, and label shown when nothing is selected. */
    ariaLabel: string;
    /** Label used for the "nothing selected" state, e.g. "All ticket types". */
    allLabel: string;
    options: MultiSelectFilterOption<T>[];
    /** Empty array means "all" (no filter applied). */
    selected: T[];
    onChange: (values: T[]) => void;
    className?: string;
}

/** A dropdown filter that lets the user select zero, one, or many options at once. */
export function MultiSelectFilter<T extends string>({
    ariaLabel,
    allLabel,
    options,
    selected,
    onChange,
    className,
}: MultiSelectFilterProps<T>) {
    function toggle(value: T, checked: boolean) {
        if (checked) {
            onChange([...selected, value]);
        } else {
            onChange(selected.filter((v) => v !== value));
        }
    }

    const triggerLabel =
        selected.length === 0
            ? allLabel
            : selected.length === 1
              ? (options.find((o) => o.value === selected[0])?.label ?? allLabel)
              : `${selected.length} selected`;

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button
                    variant="outline"
                    size="default"
                    aria-label={ariaLabel}
                    className={cn("w-[200px] justify-between font-normal", className)}
                >
                    <span className="truncate">{triggerLabel}</span>
                    <ChevronDown className="size-3.5 opacity-50" />
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-[200px]">
                {options.map((option) => (
                    <DropdownMenuCheckboxItem
                        key={option.value}
                        checked={selected.includes(option.value)}
                        onCheckedChange={(checked) => toggle(option.value, checked === true)}
                        onSelect={(e) => e.preventDefault()}
                    >
                        {option.label}
                    </DropdownMenuCheckboxItem>
                ))}
            </DropdownMenuContent>
        </DropdownMenu>
    );
}
