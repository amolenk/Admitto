using System.Reflection;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Scriban.Runtime;

namespace Amolenk.Admitto.Core.Email.Application.Templating;

internal static class EmailTemplateParameters
{
    public static IReadOnlyDictionary<string, object?> WithBranding(
        object parameters,
        EmailAccentColor accentColor,
        EmailFontFamily fontFamily)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["accent_color"] = accentColor.Value,
            ["font_family"] = fontFamily.Value
        };

        foreach (var property in parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            result[StandardMemberRenamer.Rename(property.Name)] = property.GetValue(parameters);
        }

        return result;
    }
}
