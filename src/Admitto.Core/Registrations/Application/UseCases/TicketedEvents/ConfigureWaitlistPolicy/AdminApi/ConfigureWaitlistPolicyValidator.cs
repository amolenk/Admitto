using FluentValidation;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy.AdminApi;

public sealed class ConfigureWaitlistPolicyValidator : AbstractValidator<ConfigureWaitlistPolicyHttpRequest>
{
    public ConfigureWaitlistPolicyValidator() { }
}
