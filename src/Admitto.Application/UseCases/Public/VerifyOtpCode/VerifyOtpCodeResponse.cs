namespace Amolenk.Admitto.Application.UseCases.Public.VerifyOtpCode;

/// <summary>
/// Represents a response containing a token after successfully verifying an OTP code.
/// The token can be used for a subsequent registration request.
/// If the user is already registered, their public ID and signature are also included.
/// </summary>
public record VerifyOtpCodeResponse(string RegistrationToken, Guid? PublicId = null, string? Signature = null);