using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeWithCoupon.PublicApi;

public sealed class RegisterAttendeeWithCouponValidator : AbstractValidator<RegisterAttendeeWithCouponHttpRequest>
{
    public RegisterAttendeeWithCouponValidator()
    {
        RuleFor(x => x.CouponCode)
            .MustBeParseable(CouponCode.TryFrom);

        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeIds)
            .NotNull()
            .NotEmpty();
    }
}
