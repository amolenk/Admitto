using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone.AdminApi;

public sealed class UpdateTicketedEventTimeZoneValidator : AbstractValidator<UpdateTicketedEventTimeZoneHttpRequest>
{
    public UpdateTicketedEventTimeZoneValidator()
    {
        RuleFor(x => x.TimeZone).MustBeParseable(TimeZoneId.TryFrom);
    }
}
