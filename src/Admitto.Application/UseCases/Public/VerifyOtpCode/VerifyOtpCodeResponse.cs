namespace Amolenk.Admitto.Application.UseCases.Public.VerifyOtpCode;

/// <summary>
/// Represents a response containing a token after successfully verifying an OTP code.
/// The token can be used for a subsequent registration request.
/// </summary>
public record VerifyOtpCodeResponse(string RegistrationToken);
