using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Validation;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.UpdateTicketType.AdminApi;

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

        When(x => x.ClaimWindowHours is not null, () =>
        {
            RuleFor(x => x.ClaimWindowHours!.Value)
                .GreaterThanOrEqualTo(1)
                .WithMessage("ClaimWindowHours must be at least 1.");
        });

        When(x => x.MaxReconfirmationEmails is not null, () =>
        {
            RuleFor(x => x.MaxReconfirmationEmails!.Value)
                .MustBeParseable(ReconfirmationEmailLimit.TryFrom);
        });
    }
}
