using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpHttpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
