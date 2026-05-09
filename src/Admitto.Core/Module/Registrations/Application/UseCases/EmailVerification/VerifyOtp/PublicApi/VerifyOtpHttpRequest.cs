namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public sealed record VerifyOtpHttpRequest(string Email, string Code);
