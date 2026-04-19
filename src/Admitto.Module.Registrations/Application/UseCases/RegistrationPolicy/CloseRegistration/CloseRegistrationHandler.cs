using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.CloseRegistration;

internal sealed class CloseRegistrationHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CloseRegistrationCommand>
{
    public async ValueTask HandleAsync(
        CloseRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null)
            throw new BusinessRuleViolationException(EventRegistrationPolicy.Errors.EventNotFound);

        policy.CloseForRegistration();
    }
}
