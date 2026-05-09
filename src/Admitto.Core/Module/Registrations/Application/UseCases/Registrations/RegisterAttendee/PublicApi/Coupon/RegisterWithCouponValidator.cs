using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee.PublicApi.Coupon;

public sealed class RegisterWithCouponValidator : AbstractValidator<RegisterWithCouponHttpRequest>
{
    public RegisterWithCouponValidator()
    {
        RuleFor(x => x.CouponCode)
            .NotEmpty();

        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeSlugs)
            .NotNull()
            .NotEmpty();
    }
}
