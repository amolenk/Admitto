namespace Amolenk.Admitto.Application.UseCases.Public.VerifyOtpCode;

/// <summary>
/// Represents a request to verify an OTP code sent to an email address.
/// </summary>
public record VerifyOtpCodeRequest(string Email, string Code);
