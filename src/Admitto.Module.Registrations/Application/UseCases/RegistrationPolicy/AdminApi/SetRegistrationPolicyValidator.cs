using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.AdminApi;

public sealed class SetRegistrationPolicyValidator : AbstractValidator<SetRegistrationPolicyHttpRequest>
{
    public SetRegistrationPolicyValidator()
    {
        RuleFor(x => x.RegistrationWindowClosesAt)
            .GreaterThan(x => x.RegistrationWindowOpensAt)
            .When(x => x.RegistrationWindowOpensAt.HasValue && x.RegistrationWindowClosesAt.HasValue)
            .WithMessage("Registration window close time must be after open time.");
    }
}
