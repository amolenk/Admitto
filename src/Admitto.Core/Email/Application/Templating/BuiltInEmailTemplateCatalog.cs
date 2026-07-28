using System.Reflection;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Templating;

/// <summary>
/// Ordered catalog of all built-in email templates and their defaults.
/// Built-in templates are identified by their reserved <see cref="BuiltInEmailTemplateNames"/> names.
/// </summary>
internal static class BuiltInEmailTemplateCatalog
{
    private static readonly Assembly Assembly = typeof(BuiltInEmailTemplateCatalog).Assembly;
    private const string ResourcePrefix = "Amolenk.Admitto.Core.Email.Application.Templating.Defaults.";

    // Ordered list — determines display order in the UI.
    private static readonly IReadOnlyList<EmailTemplate> Entries = BuildEntries();

    public static IReadOnlyList<EmailTemplate> All => Entries;

    public static EmailTemplate CreateTemplate(string name)
    {
        return GetByName(name)
            ?? throw new InvalidOperationException($"No built-in email template exists for name '{name}'.");
    }

    /// <summary>
    /// Finds a catalog entry by reserved name (case-insensitive). Returns null if not found.
    /// </summary>
    private static EmailTemplate? GetByName(string name) =>
        Entries.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<EmailTemplate> BuildEntries()
    {
        return
        [
            Build(BuiltInEmailTemplateNames.TicketConfirmation,
                resourceKey:  "ticket"),
            Build(BuiltInEmailTemplateNames.Reconfirmation,
                resourceKey:  "reconfirm"),
            Build(BuiltInEmailTemplateNames.Cancellation,
                resourceKey:  "cancellation"),
            Build(BuiltInEmailTemplateNames.ReconfirmCancelled,
                resourceKey:  "reconfirm-cancelled"),
            Build(BuiltInEmailTemplateNames.VisaLetterDenied,
                resourceKey:  "visa-letter-denied"),
            Build(BuiltInEmailTemplateNames.VerificationCode,
                resourceKey:  "otp-code"),
            Build(BuiltInEmailTemplateNames.WaitlistNotification,
                resourceKey:  "waitlist-notification"),
        ];
    }

    private static EmailTemplate Build(string name, string resourceKey)
    {
        var textBody = ReadEmbedded($"{ResourcePrefix}{resourceKey}.txt")
            ?? throw new InvalidOperationException(
                $"Missing embedded resource '{resourceKey}.txt' for built-in template '{name}'.");

        var htmlBody = ReadEmbedded($"{ResourcePrefix}{resourceKey}.html")
            ?? throw new InvalidOperationException(
                $"Missing embedded resource '{resourceKey}.html' for built-in template '{name}'.");

        var subject = ExtractSubject(textBody);

        return new EmailTemplate(name, subject, textBody, htmlBody);
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
