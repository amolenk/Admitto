namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Well-known, reserved names for built-in email templates.
/// These constants are the stable lookup keys used by runtime code
/// (event handlers, jobs) to identify which template to render.
/// Users cannot create custom templates with these names.
/// </summary>
public static class BuiltInEmailTemplateNames
{
    public const string TicketConfirmation = "Ticket confirmation";
    public const string Reconfirmation     = "Reconfirmation";
    public const string Cancellation       = "Cancellation";
    public const string VisaLetterDenied   = "Visa letter denied";
    public const string VerificationCode   = "Verification code";

    /// <summary>
    /// Returns true if the given name (case-insensitive) matches a reserved built-in name.
    /// </summary>
    public static bool IsReserved(string name) =>
        All.Contains(name, StringComparer.OrdinalIgnoreCase);

    internal static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        TicketConfirmation,
        Reconfirmation,
        Cancellation,
        VisaLetterDenied,
        VerificationCode,
    };
}
