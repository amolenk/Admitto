using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventCreationRejected;

internal sealed class RegisterTicketedEventCreationRejectedHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RegisterTicketedEventCreationRejectedCommand>
{
    public async ValueTask HandleAsync(
        RegisterTicketedEventCreationRejectedCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

        team?.RegisterEventCreationRejected(
            CreationRequestId.From(command.CreationRequestId),
            command.Reason,
            DateTimeOffset.UtcNow);
    }
}
