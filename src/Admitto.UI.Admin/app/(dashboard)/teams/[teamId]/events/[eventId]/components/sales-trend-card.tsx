"use client";

import { useState } from "react";
import { RegistrationListItemDto } from "@/lib/admitto-api/generated";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { ArrowUp, ArrowDown } from "lucide-react";
import {
    AreaChart,
    Area,
    XAxis,
    Tooltip,
    ResponsiveContainer,
    CartesianGrid,
} from "recharts";
import { format, subDays, startOfDay, parseISO } from "date-fns";

type Range = "14d" | "all";

interface SalesTrendCardProps {
    registrations: RegistrationListItemDto[] | undefined;
    isLoading: boolean;
}

interface DayBucket {
    date: string;
    label: string;
    registrations: number;
    cancellations: number;
}

function buildBuckets(registrations: RegistrationListItemDto[], range: Range): DayBucket[] {
    const today = startOfDay(new Date());
    const msPerDay = 1000 * 60 * 60 * 24;

    let startDay: Date;
    if (range === "14d") {
        startDay = subDays(today, 13);
    } else if (registrations.length === 0) {
        startDay = subDays(today, 13);
    } else {
        startDay = registrations.reduce((earliest, r) => {
            const d = startOfDay(parseISO(r.createdAt));
            return d < earliest ? d : earliest;
        }, startOfDay(parseISO(registrations[0].createdAt)));
    }

    const numDays = Math.max(1, Math.round((today.getTime() - startDay.getTime()) / msPerDay) + 1);
    const buckets: DayBucket[] = Array.from({ length: numDays }, (_, i) => {
        const day = new Date(startDay.getTime() + i * msPerDay);
        return {
            date: format(day, "yyyy-MM-dd"),
            label: format(day, "MMM d"),
            registrations: 0,
            cancellations: 0,
        };
    });

    const bucketMap = new Map(buckets.map((b) => [b.date, b]));

    for (const reg of registrations) {
        const key = format(parseISO(reg.createdAt), "yyyy-MM-dd");
        const bucket = bucketMap.get(key);
        if (bucket) {
            if (reg.status === "registered") {
                bucket.registrations++;
            } else if (reg.status === "cancelled") {
                bucket.cancellations++;
            }
        }
    }

    return buckets;
}

export function SalesTrendCard({ registrations, isLoading }: SalesTrendCardProps) {
    const [range, setRange] = useState<Range>("14d");

    if (isLoading) {
        return (
            <Card className="p-5">
                <Skeleton className="h-6 w-48 mb-3" />
                <Skeleton className="h-24 w-full" />
            </Card>
        );
    }

    const allRegs = registrations ?? [];
    const buckets = buildBuckets(allRegs, range);
    const total = buckets.reduce((sum, b) => sum + b.registrations, 0);
    const totalCancellations = buckets.reduce((sum, b) => sum + b.cancellations, 0);
    const hasCancellations = totalCancellations > 0;

    const prevWeek = range === "14d" ? buckets.slice(0, 7).reduce((sum, b) => sum + b.registrations, 0) : 0;
    const currWeek = range === "14d" ? buckets.slice(7).reduce((sum, b) => sum + b.registrations, 0) : 0;
    const deltaValue =
        range === "14d" && prevWeek > 0
            ? Math.round(((currWeek - prevWeek) / prevWeek) * 100)
            : null;

    const isEmpty = total === 0 && totalCancellations === 0;
    const rangeLabel = range === "14d" ? "last 14 days" : "all time";

    return (
        <Card className="p-5">
            <div className="flex items-center justify-between mb-1">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Registrations &middot; {rangeLabel}
                    </div>
                    <div className="flex items-baseline gap-3 mt-1.5">
                        <span className="font-display font-mono tabular-nums text-[32px] font-semibold leading-none">
                            {total}
                        </span>
                        {deltaValue !== null && (
                            <Badge
                                variant="outline"
                                className={
                                    deltaValue >= 0
                                        ? "text-success border-success/30 bg-success/10"
                                        : "text-amber-600 border-amber-500/30 bg-amber-500/10"
                                }
                            >
                                {deltaValue >= 0 ? (
                                    <ArrowUp className="size-3 mr-0.5" />
                                ) : (
                                    <ArrowDown className="size-3 mr-0.5" />
                                )}
                                {Math.abs(deltaValue)}% vs prior week
                            </Badge>
                        )}
                        {hasCancellations && (
                            <span className="text-xs text-muted-foreground">
                                {totalCancellations} cancelled
                            </span>
                        )}
                    </div>
                </div>
                <div className="flex items-center gap-0.5 rounded-md border p-0.5">
                    <button
                        onClick={() => setRange("14d")}
                        className={`text-[0.68rem] px-2 py-0.5 rounded-sm font-medium transition-colors ${
                            range === "14d"
                                ? "bg-primary text-primary-foreground"
                                : "text-muted-foreground hover:text-foreground"
                        }`}
                    >
                        14d
                    </button>
                    <button
                        onClick={() => setRange("all")}
                        className={`text-[0.68rem] px-2 py-0.5 rounded-sm font-medium transition-colors ${
                            range === "all"
                                ? "bg-primary text-primary-foreground"
                                : "text-muted-foreground hover:text-foreground"
                        }`}
                    >
                        All
                    </button>
                </div>
            </div>

            {isEmpty ? (
                <div className="flex items-center justify-center h-16 mt-4">
                    <p className="text-sm text-muted-foreground">No registrations yet.</p>
                </div>
            ) : (
                <>
                    <div className="mt-4 h-24">
                        <ResponsiveContainer width="100%" height="100%">
                            <AreaChart data={buckets} margin={{ top: 2, right: 2, left: 2, bottom: 2 }}>
                                <defs>
                                    <linearGradient id="salesGradient" x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor="var(--primary)" stopOpacity={0.25} />
                                        <stop offset="95%" stopColor="var(--primary)" stopOpacity={0} />
                                    </linearGradient>
                                    <linearGradient id="cancelGradient" x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor="var(--destructive)" stopOpacity={0.2} />
                                        <stop offset="95%" stopColor="var(--destructive)" stopOpacity={0} />
                                    </linearGradient>
                                </defs>
                                <CartesianGrid vertical={false} stroke="var(--border)" strokeDasharray="3 3" />
                                <XAxis dataKey="label" hide />
                                <Tooltip
                                    content={({ active, payload }) => {
                                        if (!active || !payload?.length) return null;
                                        const d = payload[0].payload as DayBucket;
                                        return (
                                            <div className="rounded-md border bg-popover px-2.5 py-1.5 text-xs shadow-sm">
                                                <div className="text-muted-foreground mb-1">{d.label}</div>
                                                <div className="flex flex-col gap-0.5">
                                                    <span>
                                                        <span className="text-muted-foreground">Registered: </span>
                                                        <span className="font-mono font-semibold">{d.registrations}</span>
                                                    </span>
                                                    {d.cancellations > 0 && (
                                                        <span>
                                                            <span className="text-muted-foreground">Cancelled: </span>
                                                            <span className="font-mono font-semibold text-destructive">{d.cancellations}</span>
                                                        </span>
                                                    )}
                                                </div>
                                            </div>
                                        );
                                    }}
                                />
                                <Area
                                    type="monotone"
                                    dataKey="registrations"
                                    stroke="var(--primary)"
                                    strokeWidth={1.75}
                                    fill="url(#salesGradient)"
                                    dot={false}
                                    activeDot={{ r: 3, fill: "var(--primary)" }}
                                />
                                {hasCancellations && (
                                    <Area
                                        type="monotone"
                                        dataKey="cancellations"
                                        stroke="var(--destructive)"
                                        strokeWidth={1.5}
                                        fill="url(#cancelGradient)"
                                        dot={false}
                                        activeDot={{ r: 3, fill: "var(--destructive)" }}
                                    />
                                )}
                            </AreaChart>
                        </ResponsiveContainer>
                    </div>
                    <div className="flex justify-between text-[10.5px] text-muted-foreground mt-1 px-0.5 font-mono tabular-nums">
                        <span>{buckets[0].label}</span>
                        <span>Today</span>
                    </div>
                </>
            )}
        </Card>
    );
}
