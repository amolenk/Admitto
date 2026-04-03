using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketedEvent.AdminApi;

public sealed class UpdateTicketedEventValidator : AbstractValidator<UpdateTicketedEventHttpRequest>
{
    public UpdateTicketedEventValidator()
    {
        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name).MustBeParseable(DisplayName.TryFrom));

        When(x => x.WebsiteUrl is not null, () =>
            RuleFor(x => x.WebsiteUrl).MustBeParseable(AbsoluteUrl.TryFrom));

        When(x => x.BaseUrl is not null, () =>
            RuleFor(x => x.BaseUrl).MustBeParseable(AbsoluteUrl.TryFrom));

        When(x => x.StartsAt.HasValue && x.EndsAt.HasValue, () =>
            RuleFor(x => x.EndsAt).GreaterThanOrEqualTo(x => x.StartsAt));
    }
}
