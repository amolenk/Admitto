using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.RemoveReconfirmPolicy;

internal sealed class RemoveReconfirmPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<RemoveReconfirmPolicyCommand>
{
    public async ValueTask HandleAsync(
        RemoveReconfirmPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var guard = await LifecycleGuardStore.LoadOrCreateAsync(writeStore, command.EventId, cancellationToken);
        guard.AssertActiveAndRegisterPolicyMutation();

        var policy = await writeStore.ReconfirmPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is not null)
        {
            writeStore.ReconfirmPolicies.Remove(policy);
        }
    }
}
