using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCancelled;

internal sealed class HandleEventCancelledHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<HandleEventCancelledCommand>
{
    public async ValueTask HandleAsync(HandleEventCancelledCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.TicketedEventId);

        var policy = await writeStore.EventRegistrationPolicies
            .FirstOrDefaultAsync(p => p.Id == eventId, cancellationToken);

        if (policy is null)
        {
            policy = EventRegistrationPolicy.Create(eventId);
            await writeStore.EventRegistrationPolicies.AddAsync(policy, cancellationToken);
        }

        policy.SetCancelled();
    }
}
