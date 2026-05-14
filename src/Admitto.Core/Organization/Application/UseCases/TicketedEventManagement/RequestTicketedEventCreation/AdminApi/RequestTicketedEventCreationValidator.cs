using Amolenk.Admitto.Core.Shared.Application.Validation;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.AdminApi;

public sealed class RequestTicketedEventCreationValidator
    : AbstractValidator<RequestTicketedEventCreationHttpRequest>
{
    public RequestTicketedEventCreationValidator()
    {
        RuleFor(x => x.Name).MustBeParseable(EventName.TryFrom);
        RuleFor(x => x.WebsiteUrl).MustBeParseable(AbsoluteUrl.TryFrom);
        RuleFor(x => x.BaseUrl).MustBeParseable(AbsoluteUrl.TryFrom);
        RuleFor(x => x.TimeZone).MustBeParseable(TimeZoneId.TryFrom);

        RuleFor(x => x.EndsAt)
            .GreaterThanOrEqualTo(x => x.StartsAt)
            .WithMessage("End date must be on or after the start date.");
    }
}
