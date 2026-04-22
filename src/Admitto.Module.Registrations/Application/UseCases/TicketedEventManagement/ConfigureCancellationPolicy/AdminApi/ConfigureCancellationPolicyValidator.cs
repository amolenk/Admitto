using FluentValidation;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy.AdminApi;

public sealed class ConfigureCancellationPolicyValidator : AbstractValidator<ConfigureCancellationPolicyHttpRequest>
{
    public ConfigureCancellationPolicyValidator()
    {
    }
}
