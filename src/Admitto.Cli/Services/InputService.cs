using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Amolenk.Admitto.Cli.Services;

public class InputService(IConfiguration configuration)
{
    public int GetPort(string text, int? defaultValue = null)
    {
        var prompt = new TextPrompt<int>(text);

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue.Value);
        }

        return AnsiConsole.Prompt(prompt);
    }

    public bool GetBoolean(string text, bool? defaultValue = null)
    {
        var confirmation = AnsiConsole.Prompt(
            new ConfirmationPrompt(text));

        return confirmation;

        // var prompt = new TextPrompt<string>(text)
        // {
        //     Choices = { "Yes", "No" }
        // };
        //
        // if (defaultValue is not null)
        // {
        //     prompt.DefaultValue(defaultValue.Value ? "Yes" : "No");
        // }
        //
        // return AnsiConsole.Prompt(prompt) == "Yes";
    }
    
    public int GetNumber(string text, int min, int max, int? defaultValue = null)
    {
        var prompt = new TextPrompt<int>(text)
        {
            Validator = i => i >= min && i <= max
                ? ValidationResult.Success() 
                : ValidationResult.Error($"Value must be between {min} and {max}")
        };

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue.Value);
        }

        return AnsiConsole.Prompt(prompt);
    }

    public DateTimeOffset GetDateTimeOffset(string text)
    {
        var date = GetDate(text);
        var time = GetTime(text);

        return new DateTimeOffset(date, time, TimeZoneInfo.Local.BaseUtcOffset);
    }

    public DateOnly GetDate(string text)
    {
        var prompt = new TextPrompt<DateOnly>($"{text} date:");
        return AnsiConsole.Prompt(prompt);
    }

    public TimeOnly GetTime(string text)
    {
        var prompt = new TextPrompt<TimeOnly>($"{text} time:");
        return AnsiConsole.Prompt(prompt);
    }

    public string GetStringFromList(string text, IEnumerable<string> choices)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"{text}:")
                .AddChoices(choices));
    }
    
    public string GetTeamSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = configuration[ConfigSettings.DefaultTeamSetting];
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Team slug must be specified.");
        }

        return value;
    }
    
    public string GetTeamSlug()
    {
        var defaultValue = configuration[ConfigSettings.DefaultTeamSetting];
        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            return defaultValue;
        }

        return GetString("Team slug", allowEmpty: false, kebaberize: true)!;
    }

    public string GetEventSlug()
    {
        var defaultValue = configuration[ConfigSettings.DefaultEventSetting];
        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            return defaultValue;
        }

        return GetString("Event slug", allowEmpty: false, kebaberize: true)!;
    }
    
    public string? GetString(
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