using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.EmailVerification.RequestOtp.PublicApi;

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpHttpRequest>
{
    public RequestOtpValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
