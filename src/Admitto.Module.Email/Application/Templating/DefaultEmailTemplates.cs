using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using System.Reflection;

namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Provides built-in default templates loaded from embedded resources.
/// </summary>
internal static class DefaultEmailTemplates
{
    private static readonly Assembly Assembly = typeof(DefaultEmailTemplates).Assembly;
    private const string ResourcePrefix = "Amolenk.Admitto.Module.Email.Application.Templating.Defaults.";

    public static EmailTemplate? Get(string type)
    {
        var textBody = ReadEmbedded($"{ResourcePrefix}{type}.txt");
        var htmlBody = ReadEmbedded($"{ResourcePrefix}{type}.html");

        if (textBody is null || htmlBody is null)
            return null;

        return EmailTemplate.Create(
            scope: EmailSettingsScope.Event,
            scopeId: Guid.Empty,
            type: type,
            subject: ExtractSubject(textBody),
            textBody: textBody,
            htmlBody: htmlBody);
    }

    private static string? ReadEmbedded(string resourceName)
    {
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ExtractSubject(string textBody)
    {
        // By convention, the first line of the text body is the subject.
        var firstLine = textBody.Split('\n', 2)[0].Trim();
        return string.IsNullOrEmpty(firstLine) ? "(no subject)" : firstLine;
    }
}
