using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCreated;

/// <summary>
/// Initialises the <see cref="EventRegistrationPolicy"/> for a newly-created ticketed
/// event. Idempotent: re-delivery of the source module event is a no-op when the policy
/// already exists.
/// </summary>
internal sealed class HandleEventCreatedHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<HandleEventCreatedCommand>
{
    public async ValueTask HandleAsync(HandleEventCreatedCommand command, CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.TicketedEventId);

        var alreadyExists = await writeStore.EventRegistrationPolicies
            .AnyAsync(p => p.Id == eventId, cancellationToken);

        if (alreadyExists) return;

        var policy = EventRegistrationPolicy.Create(eventId);
        await writeStore.EventRegistrationPolicies.AddAsync(policy, cancellationToken);
    }
}
