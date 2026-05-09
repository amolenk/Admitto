using Amolenk.Admitto.Core.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated;

internal sealed class RegisterTicketedEventCreatedHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RegisterTicketedEventCreatedCommand>
{
    public async ValueTask HandleAsync(
        RegisterTicketedEventCreatedCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null) return;

        team.RegisterEventCreated(
            CreationRequestId.From(command.CreationRequestId),
            TicketedEventId.From(command.TicketedEventId),
            DateTimeOffset.UtcNow);
    }
}
