using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.OpenRegistration;

internal sealed class OpenRegistrationHandler(
    IRegistrationsWriteStore writeStore,
    IEventEmailFacade emailFacade)
    : ICommandHandler<OpenRegistrationCommand>
{
    public async ValueTask HandleAsync(
        OpenRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null)
            throw new BusinessRuleViolationException(EventRegistrationPolicy.Errors.EventNotFound);

        var emailConfigured = await emailFacade.IsEmailConfiguredAsync(command.EventId, cancellationToken);
        if (!emailConfigured)
            throw new BusinessRuleViolationException(Errors.EmailNotConfigured);

        policy.OpenForRegistration();
    }

    internal static class Errors
    {
        public static readonly Error EmailNotConfigured = new(
            "registration.email_not_configured",
            "Email must be configured for this event before registration can be opened.",
            Type: ErrorType.Validation);
    }
}
