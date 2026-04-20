using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.SetReconfirmPolicy.AdminApi;

public sealed class SetReconfirmPolicyValidator : AbstractValidator<SetReconfirmPolicyHttpRequest>
{
    public SetReconfirmPolicyValidator()
    {
        RuleFor(x => x.OpensAt)
            .NotEmpty();

        RuleFor(x => x.ClosesAt)
            .NotEmpty()
            .GreaterThan(x => x.OpensAt)
            .WithMessage("Reconfirmation window close time must be after open time.");

        RuleFor(x => x.CadenceDays)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Reconfirmation cadence must be at least 1 day.");
    }
}
