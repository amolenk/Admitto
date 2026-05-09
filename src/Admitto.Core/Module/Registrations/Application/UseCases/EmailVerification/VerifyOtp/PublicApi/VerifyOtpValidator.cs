using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.EmailVerification.VerifyOtp.PublicApi;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpHttpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
