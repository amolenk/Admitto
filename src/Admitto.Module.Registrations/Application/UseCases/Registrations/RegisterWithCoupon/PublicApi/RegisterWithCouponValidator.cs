using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon.PublicApi;

public sealed class RegisterWithCouponValidator : AbstractValidator<RegisterWithCouponHttpRequest>
{
    public RegisterWithCouponValidator()
    {
        RuleFor(x => x.CouponCode)
            .NotEmpty();

        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty();
    }
}
