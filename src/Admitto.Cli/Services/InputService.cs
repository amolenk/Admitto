namespace Amolenk.Admitto.Cli.Services;

public static class InputService
{
    public static int GetPort(string text, int? defaultValue = null)
    {
        var prompt = new TextPrompt<int>(text);

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue.Value);
        }

        return AnsiConsole.Prompt(prompt);
    }

    public static string? GetString(
        string text,
        string? defaultValue = null,
        bool allowEmpty = false,
        bool isSecret = false,
        bool kebaberize = false)
    {
        var prompt = new TextPrompt<string>(defaultValue is null ? $"{text}:" : text)
        {
            AllowEmpty = allowEmpty,
        };

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue);
        }

        if (isSecret)
        {
            prompt.IsSecret = true;
            prompt.Mask = '*';
        }

        var result = AnsiConsole.Prompt(prompt);
        if (!kebaberize) return result;
        
        var kebabResult = result.Kebaberize();
        if (kebabResult == result) return result;
        
        AnsiConsole.MarkupLine($"{text} (URL-friendly): {kebabResult}");
        return kebabResult;
    }
}