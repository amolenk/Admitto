import { FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { cn } from "@/lib/utils"
import { format } from "date-fns"
import { Calendar as CalendarIcon } from "lucide-react"
import * as React from "react"
import { Button } from "./button"
import { Calendar } from "./calendar"
import { Popover, PopoverContent, PopoverTrigger, } from "./popover"

export function DateRangePicker({
                                    field, label
                                }: {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    field: any, label?: string
}) {
    return (
        <FormItem>
            <div>
                {label && <FormLabel>{label}</FormLabel>}
            </div>
            <Popover>
                <PopoverTrigger asChild>
                    <Button
                        id="date"
                        variant={"outline"}
                        className={cn(
                            "w-[300px] justify-start text-left font-normal",
                            !field.value && "text-muted-foreground"
                        )}
                    >
                        <CalendarIcon />
                        {field.value?.from ? (
                            field.value.to ? (
                                <>
                                    {format(field.value.from, "LLL dd, y")} -{" "}
                                    {format(field.value.to, "LLL dd, y")}
                                </>
                            ) : (
                                format(field.value.from, "LLL dd, y")
                            )
                        ) : (
                            <span>Pick a date range</span>
                        )}
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start">
                    <Calendar
                        initialFocus
                        mode="range"
                        defaultMonth={field.value?.from}
                        selected={field.value}
                        onSelect={field.onChange}
                        numberOfMonths={2}
                    />
                </PopoverContent>
            </Popover>
            <FormMessage/>
        </FormItem>
    )
}
