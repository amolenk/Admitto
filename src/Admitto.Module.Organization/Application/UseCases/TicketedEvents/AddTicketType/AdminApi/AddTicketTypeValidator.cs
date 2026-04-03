using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType.AdminApi;

public sealed class AddTicketTypeValidator : AbstractValidator<AddTicketTypeHttpRequest>
{
    public AddTicketTypeValidator()
    {
        RuleFor(x => x.Slug)
            .MustBeParseable(Slug.TryFrom);

        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        RuleForEach(x => x.TimeSlots)
            .MustBeParseable(Slug.TryFrom);

        When(x => x.Capacity.HasValue, () =>
            RuleFor(x => x.Capacity!.Value)
                .InclusiveBetween(0, 1000)
                .WithMessage("Capacity must be between 0 and 1000."));
    }
}
