namespace Amolenk.Admitto.Application.UseCases.Public.RequestOtpCode;

/// <summary>
/// Represents a request to send an OTP code to an email address for verification.
/// </summary>
public record RequestOtpCodeRequest(string Email);
    
    