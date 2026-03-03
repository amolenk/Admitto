using Amolenk.Admitto.Shared.Application.Validation;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;

public sealed class CreateTicketedEventValidator : AbstractValidator<CreateTicketedEventHttpRequest>
{
    public CreateTicketedEventValidator()
    {
        RuleFor(x => x.Slug)
            .MustBeParseable(Slug.TryFrom);

        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        RuleFor(x => x.WebsiteUrl)
            .MustBeParseable(AbsoluteUrl.TryFrom);

        RuleFor(x => x.BaseUrl)
            .MustBeParseable(AbsoluteUrl.TryFrom);

        RuleFor(x => x.StartsAt)
            .NotEmpty();

        RuleFor(x => x.EndsAt)
            .NotEmpty()
            .GreaterThan(x => x.StartsAt);
    }
}
