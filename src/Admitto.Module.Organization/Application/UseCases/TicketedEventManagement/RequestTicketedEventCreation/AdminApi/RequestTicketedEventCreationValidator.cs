using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.AdminApi;

public sealed class RequestTicketedEventCreationValidator
    : AbstractValidator<RequestTicketedEventCreationHttpRequest>
{
    public RequestTicketedEventCreationValidator()
    {
        RuleFor(x => x.Slug).MustBeParseable(Slug.TryFrom);
        RuleFor(x => x.Name).MustBeParseable(DisplayName.TryFrom);
        RuleFor(x => x.WebsiteUrl).MustBeParseable(AbsoluteUrl.TryFrom);
        RuleFor(x => x.BaseUrl).MustBeParseable(AbsoluteUrl.TryFrom);
        RuleFor(x => x.TimeZone).MustBeParseable(TimeZoneId.TryFrom);

        RuleFor(x => x.EndsAt)
            .GreaterThanOrEqualTo(x => x.StartsAt)
            .WithMessage("End date must be on or after the start date.");
    }
}
