using Amolenk.Admitto.Module.Shared.Application.Validation;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails.AdminApi;

public sealed class UpdateTicketedEventDetailsValidator : AbstractValidator<UpdateTicketedEventDetailsHttpRequest>
{
    public UpdateTicketedEventDetailsValidator()
    {
        RuleFor(x => x.Name)
            .MustBeParseable(DisplayName.TryFrom);

        RuleFor(x => x.WebsiteUrl)
            .MustBeParseable(AbsoluteUrl.TryFrom);

        RuleFor(x => x.BaseUrl)
            .MustBeParseable(AbsoluteUrl.TryFrom);

        RuleFor(x => x.EndsAt)
            .GreaterThanOrEqualTo(x => x.StartsAt)
            .WithMessage("Event end time must be on or after the start time.");
    }
}
