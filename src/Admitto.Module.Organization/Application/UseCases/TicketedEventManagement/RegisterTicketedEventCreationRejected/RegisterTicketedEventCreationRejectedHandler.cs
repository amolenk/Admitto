using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreationRejected;

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
