using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails.AdminApi;

public sealed class UpdateTicketedEventDetailsValidator : AbstractValidator<UpdateTicketedEventDetailsHttpRequest>
{
    public UpdateTicketedEventDetailsValidator()
    {
        RuleFor(x => x.Name)
            .MustBeParseable(EventName.TryFrom);

        RuleFor(x => x.WebsiteUrl)
            .MustBeParseable(AbsoluteUrl.TryFrom);

        RuleFor(x => x.BaseUrl)
            .MustBeParseable(AbsoluteUrl.TryFrom);

        RuleFor(x => x.EndsAt)
            .GreaterThanOrEqualTo(x => x.StartsAt)
            .WithMessage("Event end time must be on or after the start time.");
    }
}
