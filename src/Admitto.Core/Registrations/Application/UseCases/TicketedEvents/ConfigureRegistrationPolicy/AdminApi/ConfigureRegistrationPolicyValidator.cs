using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy.AdminApi;

public sealed class ConfigureRegistrationPolicyValidator : AbstractValidator<ConfigureRegistrationPolicyHttpRequest>
{
    public ConfigureRegistrationPolicyValidator()
    {
        When(HasAnyField, () =>
        {
            RuleFor(x => x.OpensAt)
                .NotNull()
                .WithMessage("OpensAt is required when configuring a registration policy.");

            RuleFor(x => x.ClosesAt)
                .NotNull()
                .WithMessage("ClosesAt is required when configuring a registration policy.");

            RuleFor(x => x.ClosesAt)
                .GreaterThan(x => x.OpensAt)
                .When(x => x.OpensAt is not null && x.ClosesAt is not null)
                .WithMessage("Registration window close time must be strictly after open time.");
        });

        When(x => x.AllowedEmailDomain is not null, () =>
        {
            RuleFor(x => x.AllowedEmailDomain!)
                .NotEmpty()
                .Matches(@"^@[^\s@]+\.[^\s@]+$")
                .WithMessage("Allowed email domain must be of the form '@example.com'.");
        });
    }

    private static bool HasAnyField(ConfigureRegistrationPolicyHttpRequest r) =>
        r.OpensAt is not null
        || r.ClosesAt is not null
        || r.AllowedEmailDomain is not null;
}
