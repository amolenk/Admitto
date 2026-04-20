using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy;

internal sealed class SetCancellationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<SetCancellationPolicyCommand>
{
    public async ValueTask HandleAsync(
        SetCancellationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var guard = await LifecycleGuardStore.LoadOrCreateAsync(writeStore, command.EventId, cancellationToken);
        guard.AssertActiveAndRegisterPolicyMutation();

        var policy = await writeStore.CancellationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null)
        {
            policy = Domain.Entities.CancellationPolicy.Create(
                command.EventId, command.LateCancellationCutoff);
            await writeStore.CancellationPolicies.AddAsync(policy, cancellationToken);
        }
        else
        {
            policy.UpdateCutoff(command.LateCancellationCutoff);
        }
    }
}
