namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public sealed record VerifyOtpHttpRequest(string Email, string Code);
