using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy.AdminApi;

public sealed class ConfigureRegistrationPolicyValidator : AbstractValidator<ConfigureRegistrationPolicyHttpRequest>
{
    public ConfigureRegistrationPolicyValidator()
    {
        RuleFor(x => x.ClosesAt)
            .GreaterThan(x => x.OpensAt)
            .WithMessage("Registration window close time must be strictly after open time.");

        When(x => x.AllowedEmailDomain is not null, () =>
        {
            RuleFor(x => x.AllowedEmailDomain!)
                .NotEmpty()
                .Matches(@"^@[^\s@]+\.[^\s@]+$")
                .WithMessage("Allowed email domain must be of the form '@example.com'.");
        });
    }
}
