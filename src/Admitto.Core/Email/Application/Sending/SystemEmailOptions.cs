using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Core.Email.Application.Sending;

public sealed class SystemEmailOptions
{
    public const string SectionName = "Email:System";

    [Required]
    public string SmtpHost { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;

    [Required]
    public string FromAddress { get; init; } = string.Empty;

    public string AuthMode { get; init; } = "None";

    public string? Username { get; init; }

    public string? Password { get; init; }
}
