using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy;

internal sealed class SetRegistrationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<SetRegistrationPolicyCommand>
{
    public async ValueTask HandleAsync(
        SetRegistrationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == command.EventId, cancellationToken);

        if (policy is null)
        {
            policy = EventRegistrationPolicy.Create(command.EventId);
            writeStore.EventRegistrationPolicies.Add(policy);
        }

        if (command.RegistrationWindowOpensAt.HasValue && command.RegistrationWindowClosesAt.HasValue)
        {
            policy.SetWindow(command.RegistrationWindowOpensAt.Value, command.RegistrationWindowClosesAt.Value);
        }
        else
        {
            policy.ClearWindow();
        }

        policy.SetDomainRestriction(command.AllowedEmailDomain);
    }
}
