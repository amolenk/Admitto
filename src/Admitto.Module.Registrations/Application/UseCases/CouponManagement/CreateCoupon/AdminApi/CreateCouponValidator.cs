using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponHttpRequest>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Email)
            .MustBeParseable(EmailAddress.TryFrom);

        RuleFor(x => x.AllowedTicketTypeSlugs)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.AllowedTicketTypeSlugs)
            .MustBeParseable(Slug.TryFrom);

        RuleFor(x => x.ExpiresAt)
            .NotEmpty();
    }
}
