using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration.PartnerApi;

public sealed class UpdatePartnerRegistrationValidator : AbstractValidator<UpdatePartnerRegistrationHttpRequest>
{
    public UpdatePartnerRegistrationValidator()
    {
        RuleFor(x => x.FirstName)
            .MustBeParseable(FirstName.TryFrom);

        RuleFor(x => x.LastName)
            .MustBeParseable(LastName.TryFrom);

        RuleFor(x => x.TicketTypeIds)
            .NotNull()
            .Must(ids => ids is { Length: > 0 })
            .WithMessage("At least one ticket type must be specified.");

        RuleFor(x => x.WaitlistCouponCode!.Value)
            .MustBeParseable(CouponCode.TryFrom)
            .When(x => x.WaitlistCouponCode.HasValue);
    }
}
