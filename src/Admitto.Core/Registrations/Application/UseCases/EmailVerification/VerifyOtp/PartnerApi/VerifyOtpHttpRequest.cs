namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PartnerApi;

public sealed record VerifyOtpHttpRequest(string Email, string Code);
