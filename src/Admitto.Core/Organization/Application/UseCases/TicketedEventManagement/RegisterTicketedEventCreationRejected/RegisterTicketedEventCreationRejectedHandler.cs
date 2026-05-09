using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;

internal sealed class RegisterTicketedEventCreationRejectedHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RegisterTicketedEventCreationRejectedCommand>
{
    public async ValueTask HandleAsync(
        RegisterTicketedEventCreationRejectedCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null) return;

        team.RegisterEventCreationRejected(
            CreationRequestId.From(command.CreationRequestId),
            command.Reason,
            DateTimeOffset.UtcNow);
    }
}
