import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";

interface FormAlertProps
{
    error: { title: string; detail: string };
}

export function FormAlert({ error }: FormAlertProps)
{
    return (
        <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertTitle className="text-red-500">{error.title}</AlertTitle>
            <AlertDescription>{error.detail}</AlertDescription>
        </Alert>
    );
}