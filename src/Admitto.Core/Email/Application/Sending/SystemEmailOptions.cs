using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Core.Email.Application.Sending;

public sealed class SystemEmailOptions
{
    public const string SectionName = "Email:System";

    [Required]
    public string SmtpHost { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;

    public bool SmtpSsl { get; init; }

    public bool SmtpStartTls { get; init; }

    [Required]
    public string FromAddress { get; init; } = string.Empty;

    /// <summary>
    /// Visible MIME <c>From</c> display name. The platform always sends under its own
    /// identity rather than a team's, so this is deployment configuration and is never
    /// derived from team data.
    /// </summary>
    public string FromDisplayName { get; init; } = "Admitto";

    public string AuthMode { get; init; } = "None";

    public string? Username { get; init; }

    public string? Password { get; init; }
}
