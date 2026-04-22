using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventCreated;

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
