using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RegisterTicketedEventArchived;

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
