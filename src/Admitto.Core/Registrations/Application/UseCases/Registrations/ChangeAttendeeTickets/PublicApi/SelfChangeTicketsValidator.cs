using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PublicApi;

public sealed class SelfChangeTicketsValidator : AbstractValidator<SelfChangeTicketsHttpRequest>
{
    public SelfChangeTicketsValidator()
    {
        RuleFor(x => x.TicketTypeIds)
            .NotNull();

        RuleFor(x => x.WaitlistCouponCode!.Value)
            .MustBeParseable(CouponCode.TryFrom)
            .When(x => x.WaitlistCouponCode.HasValue);
    }
}
