import { FormLabel } from "@/components/ui/form";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { InfoIcon } from "lucide-react";

interface FormTooltipLabelProps
{
    labelText: string;
    helpText: string;
}

export function FormTooltipLabel({ labelText, helpText }: FormTooltipLabelProps)
{
    return (
        <div className="inline-flex items-center gap-1">
            <FormLabel className="text-sm font-medium text-gray-700">{labelText}</FormLabel>
            <TooltipProvider>
                <Tooltip>
                    <TooltipTrigger>
                        <InfoIcon size={16} />
                    </TooltipTrigger>
                    <TooltipContent>
                        <p>{helpText}</p>
                    </TooltipContent>
                </Tooltip>
            </TooltipProvider>
        </div>
    );
}
