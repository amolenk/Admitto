using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public sealed class UpdateTicketTypeValidator : AbstractValidator<UpdateTicketTypeHttpRequest>
{
    public UpdateTicketTypeValidator()
    {
        RuleFor(x => x.Name)
            .MustBeNullOrParseable(DisplayName.TryFrom);

        When(x => x.MaxCapacity is not null, () =>
        {
            RuleFor(x => x.MaxCapacity!.Value)
                .GreaterThan(0);
        });
    }
}
