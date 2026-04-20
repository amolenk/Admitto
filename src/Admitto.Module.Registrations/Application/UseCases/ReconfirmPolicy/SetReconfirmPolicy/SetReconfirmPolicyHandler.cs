using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.SetReconfirmPolicy;

internal sealed class SetReconfirmPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<SetReconfirmPolicyCommand>
{
    public async ValueTask HandleAsync(
        SetReconfirmPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var guard = await LifecycleGuardStore.LoadOrCreateAsync(writeStore, command.EventId, cancellationToken);
        guard.AssertActiveAndRegisterPolicyMutation();

        var policy = await writeStore.ReconfirmPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null)
        {
            policy = Domain.Entities.ReconfirmPolicy.Create(
                command.EventId, command.OpensAt, command.ClosesAt, command.Cadence);
            await writeStore.ReconfirmPolicies.AddAsync(policy, cancellationToken);
        }
        else
        {
            policy.Update(command.OpensAt, command.ClosesAt, command.Cadence);
        }
    }
}
