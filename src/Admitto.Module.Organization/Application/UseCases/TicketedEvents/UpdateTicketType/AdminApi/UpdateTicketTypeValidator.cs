using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketType.AdminApi;

public sealed class UpdateTicketTypeValidator : AbstractValidator<UpdateTicketTypeHttpRequest>
{
    public UpdateTicketTypeValidator()
    {
        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        When(x => x.Capacity.HasValue, () =>
            RuleFor(x => x.Capacity!.Value)
                .InclusiveBetween(0, 10_000)
                .WithMessage("Capacity must be between 0 and 10,000."));
    }
}
