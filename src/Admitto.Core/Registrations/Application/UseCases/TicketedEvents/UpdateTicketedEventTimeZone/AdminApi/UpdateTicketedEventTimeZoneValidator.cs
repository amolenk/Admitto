using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventTimeZone.AdminApi;

public sealed class UpdateTicketedEventTimeZoneValidator : AbstractValidator<UpdateTicketedEventTimeZoneHttpRequest>
{
    public UpdateTicketedEventTimeZoneValidator()
    {
        RuleFor(x => x.TimeZone).MustBeParseable(TimeZoneId.TryFrom);
    }
}
