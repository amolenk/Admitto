using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

internal sealed class ConfigureCancellationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureCancellationPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureCancellationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            eventId,
            command.ExpectedVersion,
            cancellationToken);

        var policy = command.LateCancellationCutoff is { } cutoff
            ? new TicketedEventCancellationPolicy(cutoff)
            : null;

        ticketedEvent.ConfigureCancellationPolicy(policy);
    }
}
