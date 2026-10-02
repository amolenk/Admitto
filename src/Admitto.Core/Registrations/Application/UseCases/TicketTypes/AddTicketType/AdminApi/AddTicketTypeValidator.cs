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

        // A public capacity of 0 means sold out to self-service; admin tickets come on top of it.
        When(x => x.PublicCapacity is not null, () =>
        {
            RuleFor(x => x.PublicCapacity!.Value)
                .GreaterThanOrEqualTo(0);
        });

        When(x => x.WaitlistEnabled, () =>
        {
            RuleFor(x => x.PublicCapacity)
                .NotNull()
                .WithMessage("WaitlistEnabled requires a bounded capacity (PublicCapacity must be set).");
        });

        RuleFor(x => x.ClaimWindowHours)
            .GreaterThanOrEqualTo(1)
            .WithMessage("ClaimWindowHours must be at least 1.");

        When(x => x.MaxReconfirmationEmails is not null, () =>
        {
            RuleFor(x => x.MaxReconfirmationEmails!.Value)
                .MustBeParseable(ReconfirmationEmailLimit.TryFrom);
        });
    }
}
