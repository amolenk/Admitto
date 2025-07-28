import { TrendingDownIcon, TrendingUpIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

export function SectionCards()
{
    return (
        <div
            className="*:data-[slot=card]:shadow-xs @xl/main:grid-cols-2 @5xl/main:grid-cols-3 grid grid-cols-1 gap-4 px-4 *:data-[slot=card]:bg-gradient-to-t *:data-[slot=card]:from-primary/5 *:data-[slot=card]:to-card dark:*:data-[slot=card]:bg-card lg:px-6">
            <Card className="@container/card">
                <CardHeader className="relative">
                    <CardDescription>Event start</CardDescription>
                    <CardTitle className="@[250px]/card:text-3xl text-2xl font-semibold tabular-nums">
                        100 day(s)
                    </CardTitle>
                </CardHeader>
                {/*<CardFooter className="flex-col items-start gap-1 text-sm">*/}
                {/*    <div className="line-clamp-1 flex gap-2 font-medium">*/}
                {/*        Trending up this month <TrendingUpIcon className="size-4" />*/}
                {/*    </div>*/}
                {/*    <div className="text-muted-foreground">*/}
                {/*        Visitors for the last 6 months*/}
                {/*    </div>*/}
                {/*</CardFooter>*/}
            </Card>
            <Card className="@container/card">
                <CardHeader className="relative">
                    <CardDescription>Registrations</CardDescription>
                    <CardTitle className="@[250px]/card:text-3xl text-2xl font-semibold tabular-nums">
                        123
                    </CardTitle>
                </CardHeader>
                {/*<CardFooter className="flex-col items-start gap-1 text-sm">*/}
                {/*    <div className="line-clamp-1 flex gap-2 font-medium">*/}
                {/*        Down 20% this period <TrendingDownIcon className="size-4" />*/}
                {/*    </div>*/}
                {/*    <div className="text-muted-foreground">*/}
                {/*        Acquisition needs attention*/}
                {/*    </div>*/}
                {/*</CardFooter>*/}
            </Card>
            <Card className="@container/card">
                <CardHeader className="relative">
                    <CardDescription>Confirmations</CardDescription>
                    <CardTitle className="@[250px]/card:text-3xl text-2xl font-semibold tabular-nums">
                        50/100
                    </CardTitle>
                </CardHeader>
            </Card>
        </div>
    );
}