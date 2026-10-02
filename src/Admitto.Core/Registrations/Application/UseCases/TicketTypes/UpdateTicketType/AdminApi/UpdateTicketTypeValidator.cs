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

        // A public capacity of 0 means sold out to self-service; admin tickets come on top of it.
        When(x => x.PublicCapacity is not null, () =>
        {
            RuleFor(x => x.PublicCapacity!.Value)
                .GreaterThanOrEqualTo(0);
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
