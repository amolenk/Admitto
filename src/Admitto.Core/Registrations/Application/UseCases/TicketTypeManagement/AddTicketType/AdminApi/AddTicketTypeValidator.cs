using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;

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
    }
}
