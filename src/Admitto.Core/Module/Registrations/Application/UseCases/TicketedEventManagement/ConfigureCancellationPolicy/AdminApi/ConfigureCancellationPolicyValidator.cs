using FluentValidation;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy.AdminApi;

public sealed class ConfigureCancellationPolicyValidator : AbstractValidator<ConfigureCancellationPolicyHttpRequest>
{
    public ConfigureCancellationPolicyValidator()
    {
    }
}
