using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

internal sealed class ConfigureCancellationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureCancellationPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureCancellationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);

        var policy = command.LateCancellationCutoff is { } cutoff
            ? new TicketedEventCancellationPolicy(cutoff)
            : null;

        ticketedEvent.ConfigureCancellationPolicy(policy);
    }
}
