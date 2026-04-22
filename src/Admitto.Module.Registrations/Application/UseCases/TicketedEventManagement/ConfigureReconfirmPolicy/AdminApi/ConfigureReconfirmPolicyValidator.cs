using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy.AdminApi;

public sealed class ConfigureReconfirmPolicyValidator : AbstractValidator<ConfigureReconfirmPolicyHttpRequest>
{
    public ConfigureReconfirmPolicyValidator()
    {
        When(HasAnyField, () =>
        {
            RuleFor(x => x.OpensAt)
                .NotNull()
                .WithMessage("OpensAt is required when configuring a reconfirm policy.");

            RuleFor(x => x.ClosesAt)
                .NotNull()
                .WithMessage("ClosesAt is required when configuring a reconfirm policy.");

            RuleFor(x => x.CadenceDays)
                .NotNull()
                .WithMessage("CadenceDays is required when configuring a reconfirm policy.")
                .GreaterThanOrEqualTo(1)
                .When(x => x.CadenceDays is not null)
                .WithMessage("Reconfirmation cadence must be at least 1 day.");

            RuleFor(x => x.ClosesAt)
                .GreaterThan(x => x.OpensAt)
                .When(x => x.OpensAt is not null && x.ClosesAt is not null)
                .WithMessage("Reconfirmation window close time must be strictly after open time.");
        });
    }

    private static bool HasAnyField(ConfigureReconfirmPolicyHttpRequest r) =>
        r.OpensAt is not null || r.ClosesAt is not null || r.CadenceDays is not null;
}
