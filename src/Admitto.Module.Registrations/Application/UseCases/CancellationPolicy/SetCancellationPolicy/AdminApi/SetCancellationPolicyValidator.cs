using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy.AdminApi;

public sealed class SetCancellationPolicyValidator : AbstractValidator<SetCancellationPolicyHttpRequest>
{
    public SetCancellationPolicyValidator()
    {
    }
}
