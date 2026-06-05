using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpHttpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
