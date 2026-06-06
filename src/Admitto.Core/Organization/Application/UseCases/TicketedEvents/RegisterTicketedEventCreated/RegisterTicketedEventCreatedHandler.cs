using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreated;

internal sealed class RegisterTicketedEventCreatedHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RegisterTicketedEventCreatedCommand>
{
    public async ValueTask HandleAsync(
        RegisterTicketedEventCreatedCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        team?.RegisterEventCreated(
            CreationRequestId.From(command.CreationRequestId),
            TicketedEventId.From(command.TicketedEventId),
            DateTimeOffset.UtcNow);
    }
}
