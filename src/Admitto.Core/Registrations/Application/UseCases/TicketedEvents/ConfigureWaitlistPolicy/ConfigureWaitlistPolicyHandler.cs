using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy;

internal sealed class ConfigureWaitlistPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureWaitlistPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureWaitlistPolicyCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.ConfigureWaitlistPolicy(command.QuietHoursStart, command.QuietHoursEnd);
    }
}
