using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RegisterTicketedEventArchived;

internal sealed class RegisterTicketedEventArchivedHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RegisterTicketedEventArchivedCommand>
{
    public async ValueTask HandleAsync(
        RegisterTicketedEventArchivedCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);

        var team = await writeStore.Teams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null) return;

        team.RegisterEventArchived(TicketedEventId.From(command.TicketedEventId));
    }
}
