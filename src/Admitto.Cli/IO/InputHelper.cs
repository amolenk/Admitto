using Amolenk.Admitto.Cli.Configuration;

namespace Amolenk.Admitto.Cli.IO;

public static class InputHelper
{
    public static string ResolveTeamSlug(string? teamSlug, IConfigService configService)
    {
        if (teamSlug is not null)
        {
            return teamSlug;
        }

        if (configService.DefaultTeam is not null)
        {
            return configService.DefaultTeam;
        }
        
        throw new ArgumentException("Team slug must be specified.");
    }

    public static string ResolveEventSlug(string? eventSlug, IConfigService configService)
    {
        if (eventSlug is not null)
        {
            return eventSlug;
        }

        if (configService.DefaultEvent is not null)
        {
            return configService.DefaultEvent;
        }
        
        throw new ArgumentException("Event slug must be specified.");
    }

    // Quarantined: ParseAdditionalDetails / ParseTickets depended on AdditionalDetailDto / TicketSelectionDto,
    // which were dropped from the API surface. Restore alongside the corresponding admin endpoints.

    [Obsolete]
    public static List<TItem> Parse<TItem>(string[]? input, Func<string, string, TItem> createItem)
    {
        return Parse<TItem, string>(input, createItem);
    }

    [Obsolete]
    public static List<TItem> Parse<TItem, TValue>(string[]? input, Func<string, TValue, TItem> createItem)
    {
        var result = new List<TItem>();        
        
        foreach (var item in input ?? [])
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                var typedValue = (TValue)Parse<TValue>(parts[1]);
                
                result.Add(createItem(parts[0], typedValue));
            }
            else
            {
                throw new Exception($"Invalid format '{item}'. Expected format is 'key=value'.");
            }
        }

        return result;
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