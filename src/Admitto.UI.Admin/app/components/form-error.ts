import {HttpValidationProblemDetails} from "@/lib/admitto-api/generated";

export class FormError extends Error
{
    public status: number;
    public title: string;
    public detail: string;
    public errors: Record<string, string[]> | null;

    constructor(public problemDetails: HttpValidationProblemDetails)
    {
        super(problemDetails.title ?? "Server Error");
        this.name = "FormError";

        this.status = problemDetails.status as number ?? 500;
        this.title = problemDetails.title ?? "Server Error";
        this.detail = problemDetails.detail ?? "An unexpected error occurred.";
        this.errors = problemDetails.errors ?? {};
    }
}
