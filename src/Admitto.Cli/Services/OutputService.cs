using Microsoft.Kiota.Abstractions.Serialization;

namespace Amolenk.Admitto.Cli.Services;

public static class OutputService
{
    public static void WriteSuccesMessage(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓ {message}[/]");
    }
    
    public static void WriteException(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]{ex.Message.EscapeMarkup()}[/]");
    }
    
    public static void WriteException(ProblemDetails ex)
    {
        AnsiConsole.MarkupLine(
            !string.IsNullOrWhiteSpace(ex.Detail)
                ? $"[red]{ex.Title.EscapeMarkup()}:[/] {ex.Detail.EscapeMarkup()}"
                : $"[red]{ex.Title.EscapeMarkup()}[/]");
    }
    
    public static void WriteException(HttpValidationProblemDetails ex)
    {
        AnsiConsole.MarkupLine(
            !string.IsNullOrWhiteSpace(ex.Detail)
                ? $"[red]{ex.Title.EscapeMarkup()}:[/] {ex.Detail.EscapeMarkup()}"
                : $"[red]Error: {ex.Title.EscapeMarkup()}[/]");

        if (ex.Errors is null) return;
        
        // https://github.com/microsoft/kiota/issues/62
        foreach (var error in ex.Errors.AdditionalData)
        {
            foreach (var errorValue in ((UntypedArray)error.Value).GetValue())
            {
                AnsiConsole.WriteLine(
                    $"- {((UntypedString)errorValue).GetValue().EscapeMarkup()}");
            }
        }
    }
}