import { Separator } from "@/components/ui/separator";

interface FormHeadingProps
{
    text: string;
}

export function FormHeading({ text }: FormHeadingProps)
{
    return (
        <div>
            <h2 className="text-2xl my-1 dark:text-white">{text}</h2>
            <Separator />
        </div>
    );
}
