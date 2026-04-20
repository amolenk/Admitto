using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.RemoveCancellationPolicy;

internal sealed class RemoveCancellationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<RemoveCancellationPolicyCommand>
{
    public async ValueTask HandleAsync(
        RemoveCancellationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var guard = await LifecycleGuardStore.LoadOrCreateAsync(writeStore, command.EventId, cancellationToken);
        guard.AssertActiveAndRegisterPolicyMutation();

        var policy = await writeStore.CancellationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is not null)
        {
            writeStore.CancellationPolicies.Remove(policy);
        }
    }
}
