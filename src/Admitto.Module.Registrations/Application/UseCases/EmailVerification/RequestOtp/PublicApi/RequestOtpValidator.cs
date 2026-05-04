using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EmailVerification.RequestOtp.PublicApi;

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpHttpRequest>
{
    public RequestOtpValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
