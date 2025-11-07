namespace Amolenk.Admitto.Cli.Common;

public static class AnsiConsoleExt
{
    public static void WriteSuccesMessage(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓ {message.EscapeMarkup()}[/]");
    }
    
    public static void WriteErrorMessage(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗ {message.EscapeMarkup()}[/]");
    }

    public static void WriteException(ProblemDetails ex)
    {
        WriteErrorMessage(ex.Detail!);
    }

    public static void WriteException(HttpValidationProblemDetails ex)
    {
        WriteErrorMessage(ex.Detail!);
    }

    public static void WriteException(Exception ex)
    {
        WriteErrorMessage(ex.Message);
    }
}