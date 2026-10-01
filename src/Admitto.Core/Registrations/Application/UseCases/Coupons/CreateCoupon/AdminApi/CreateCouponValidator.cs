using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.CreateCoupon.AdminApi;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponHttpRequest>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.AllowedTicketTypeIds)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.AllowedTicketTypeIds)
            .Must(ids => ids.Distinct().Count() == ids.Length)
            .When(x => x.AllowedTicketTypeIds is { Length: > 0 })
            .WithMessage("'AllowedTicketTypeIds' must not contain duplicate ticket type ids.");

        RuleFor(x => x.ExpiresAt)
            .NotEmpty();
    }
}
