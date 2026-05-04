namespace Amolenk.Admitto.Module.Registrations.Application.Security;

internal sealed class OtpOptions
{
    public const string SectionName = "Registrations:Otp";

    /// <summary>OTP code lifetime in minutes. Defaults to 10.</summary>
    public int ExpiryMinutes { get; set; } = 10;

    /// <summary>Rate limit window in minutes. Defaults to 10.</summary>
    public int RateLimitWindowMinutes { get; set; } = 10;

    /// <summary>Maximum OTP requests per email+event within the rate limit window. Defaults to 3.</summary>
    public int MaxRequestsPerWindow { get; set; } = 3;
}
