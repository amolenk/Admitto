using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;

public sealed class AddTicketTypeValidator : AbstractValidator<AddTicketTypeHttpRequest>
{
    public AddTicketTypeValidator()
    {
        RuleFor(x => x.Slug)
            .MustBeParseable(Slug.TryFrom);

        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        When(x => x.TimeSlots is not null, () =>
        {
            RuleForEach(x => x.TimeSlots!)
                .MustBeParseable(Slug.TryFrom);
        });

        When(x => x.MaxCapacity is not null, () =>
        {
            RuleFor(x => x.MaxCapacity!.Value)
                .GreaterThan(0);
        });
    }
}
