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

        RuleFor(x => x.RegisterTicketTypeIds)
            .NotNull();

        RuleFor(x => x.WaitlistTicketTypeIds)
            .NotNull();

        RuleFor(x => x)
            .Must(x => (x.RegisterTicketTypeIds?.Length ?? 0) > 0 || (x.WaitlistTicketTypeIds?.Length ?? 0) > 0)
            .WithMessage("At least one registration or waitlist ticket type must be specified.");

        RuleFor(x => x.CouponCode!.Value)
            .MustBeParseable(Domain.ValueObjects.CouponCode.TryFrom)
            .When(x => x.CouponCode.HasValue);
    }
}
