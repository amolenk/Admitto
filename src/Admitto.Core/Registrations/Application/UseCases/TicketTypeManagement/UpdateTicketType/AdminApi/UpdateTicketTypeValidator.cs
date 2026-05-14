using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public sealed class UpdateTicketTypeValidator : AbstractValidator<UpdateTicketTypeHttpRequest>
{
    public UpdateTicketTypeValidator()
    {
        RuleFor(x => x.Name)
            .MustBeNullOrParseable(TicketTypeName.TryFrom);

        When(x => x.MaxCapacity is not null, () =>
        {
            RuleFor(x => x.MaxCapacity!.Value)
                .GreaterThan(0);
        });
    }
}
