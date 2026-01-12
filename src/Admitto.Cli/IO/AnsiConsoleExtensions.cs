using Amolenk.Admitto.Cli.Api;

namespace Amolenk.Admitto.Cli.IO;

public static class AnsiConsoleExt
{
    public static bool Confirm(string message)
    {
        var prompt = new ConfirmationPrompt(message)
        {
            DefaultValue = false
        };

        return AnsiConsole.Prompt(prompt);
    }
    
    public static void WriteSuccesMessage(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓ {message.EscapeMarkup()}[/]");
    }

    public static void WriteWarningMessage(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]! {message.EscapeMarkup()}[/]");
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