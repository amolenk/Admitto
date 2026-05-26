using System.Reflection;

namespace Amolenk.Admitto.Core.Email.Application.Templating;

/// <summary>
/// Represents a single built-in email template entry in the catalog.
/// </summary>
internal sealed record BuiltInEmailTemplateCatalogEntry(
    string Name,
    string Description,
    string DefaultSubject,
    string DefaultTextBody,
    string DefaultHtmlBody);

/// <summary>
/// Ordered catalog of all built-in email templates and their defaults.
/// Built-in templates are identified by their reserved <see cref="BuiltInEmailTemplateNames"/> names.
/// </summary>
internal static class BuiltInEmailTemplateCatalog
{
    private static readonly Assembly Assembly = typeof(BuiltInEmailTemplateCatalog).Assembly;
    private const string ResourcePrefix = "Amolenk.Admitto.Core.Email.Application.Templating.Defaults.";

    // Ordered list — determines display order in the UI.
    private static readonly IReadOnlyList<BuiltInEmailTemplateCatalogEntry> _entries = BuildEntries();

    public static IReadOnlyList<BuiltInEmailTemplateCatalogEntry> All => _entries;

    /// <summary>
    /// Finds a catalog entry by reserved name (case-insensitive). Returns null if not found.
    /// </summary>
    public static BuiltInEmailTemplateCatalogEntry? GetByName(string name) =>
        _entries.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<BuiltInEmailTemplateCatalogEntry> BuildEntries()
    {
        return
        [
            Build(BuiltInEmailTemplateNames.TicketConfirmation,
                description:  "Sent after successful registration",
                resourceKey:  "ticket"),
            Build(BuiltInEmailTemplateNames.Reconfirmation,
                description:  "One-week-out reconfirmation request",
                resourceKey:  "reconfirm"),
            Build(BuiltInEmailTemplateNames.Cancellation,
                description:  "Sent when an attendee cancels",
                resourceKey:  "cancellation"),
            Build(BuiltInEmailTemplateNames.ReconfirmCancelled,
                description:  "Sent when a registration is auto-cancelled after no reconfirmation response",
                resourceKey:  "reconfirm-cancelled"),
            Build(BuiltInEmailTemplateNames.VisaLetterDenied,
                description:  "Sent when a visa letter request is declined",
                resourceKey:  "visa-letter-denied"),
            Build(BuiltInEmailTemplateNames.VerificationCode,
                description:  "Sent when someone starts registration",
                resourceKey:  "otp-code"),
            Build(BuiltInEmailTemplateNames.WaitlistNotification,
                description:  "Sent when a waitlist spot becomes available",
                resourceKey:  "waitlist-notification"),
        ];
    }

    private static BuiltInEmailTemplateCatalogEntry Build(
        string name,
        string description,
        string resourceKey)
    {
        var textBody = ReadEmbedded($"{ResourcePrefix}{resourceKey}.txt")
            ?? throw new InvalidOperationException(
                $"Missing embedded resource '{resourceKey}.txt' for built-in template '{name}'.");

        var htmlBody = ReadEmbedded($"{ResourcePrefix}{resourceKey}.html")
            ?? throw new InvalidOperationException(
                $"Missing embedded resource '{resourceKey}.html' for built-in template '{name}'.");

        var subject = ExtractSubject(textBody);

        return new BuiltInEmailTemplateCatalogEntry(name, description, subject, textBody, htmlBody);
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
        // By convention the first line of the text body is the subject.
        var firstLine = textBody.Split('\n', 2)[0].Trim();
        return string.IsNullOrEmpty(firstLine) ? "(no subject)" : firstLine;
    }
}
