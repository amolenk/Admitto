using Microsoft.Kiota.Abstractions.Serialization;

namespace Amolenk.Admitto.Cli.Services;

public static class AdditionalDataExtensions
{
    public static T Parse<T>(this T additionalDataHolder, string[]? additionalData)
        where T : IAdditionalDataHolder
    {
        if (additionalData is null) return additionalDataHolder;

        foreach (var item in additionalData)
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                additionalDataHolder.AdditionalData[parts[0]] = parts[1];
            }
            else
            {
                throw new Exception($"Invalid format '{item}'. Expected format is 'key=value'.");
                // AnsiConsole.MarkupLine(
                //     $"[red]Invalid format '{item}'. Expected format is 'key=value'.[/]");
            }
        }

        return additionalDataHolder;
    }
    
    public static T Parse<T, TValue>(this T additionalDataHolder, string[]? additionalData)
        where T : IAdditionalDataHolder
    {
        if (additionalData is null) return additionalDataHolder;

        foreach (var item in additionalData)
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                var typedValue = Parse<TValue>(parts[1]);
                
                additionalDataHolder.AdditionalData[parts[0]] = typedValue;
            }
            else
            {
                throw new Exception($"Invalid format '{item}'. Expected format is 'key=value'.");
            }
        }

        return additionalDataHolder;
    }

    private static object Parse<T>(string value)
    {
        if (typeof(T) == typeof(Guid))
        {
            if (Guid.TryParse(value, out var guid))
            {
                return guid;
            }

            throw new ArgumentException($"'{value}' is not a valid GUID.");
        }

        return Convert.ChangeType(value, typeof(T));
    }
}

    