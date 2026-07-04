using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.AddTicketType.AdminApi;

public sealed class AddTicketTypeValidator : AbstractValidator<AddTicketTypeHttpRequest>
{
    public AddTicketTypeValidator()
    {
        RuleFor(x => x.Name)
            .MustBeParseable(TicketTypeName.TryFrom);

        When(x => x.TimeSlots is not null, () =>
        {
            RuleForEach(x => x.TimeSlots!)
                .MustBeParseable(TimeSlot.TryFrom);
        });

        When(x => x.MaxCapacity is not null, () =>
        {
            RuleFor(x => x.MaxCapacity!.Value)
                .GreaterThan(0);
        });

        When(x => x.WaitlistEnabled, () =>
        {
            RuleFor(x => x.MaxCapacity)
                .NotNull()
                .WithMessage("WaitlistEnabled requires a bounded capacity (MaxCapacity must be set).");
        });

        RuleFor(x => x.ClaimWindowHours)
            .GreaterThanOrEqualTo(1)
            .WithMessage("ClaimWindowHours must be at least 1.");

        When(x => x.MaxReconfirmAttempts is not null, () =>
        {
            RuleFor(x => x.MaxReconfirmAttempts!.Value)
                .GreaterThanOrEqualTo(1)
                .WithMessage("MaxReconfirmAttempts must be at least 1.");
        });
    }
}
