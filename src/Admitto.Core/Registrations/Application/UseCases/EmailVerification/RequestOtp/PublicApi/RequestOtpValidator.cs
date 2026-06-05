using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.EmailVerification.RequestOtp.PublicApi;

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpHttpRequest>
{
    public RequestOtpValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);
    }
}
