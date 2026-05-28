using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMemberships.EventHandlers;

internal sealed class TeamArchivedDomainEventHandler(ICommandHandler<RemoveTeamMembershipsCommand> removeTeamMembershipsHandler)
    : IDomainEventHandler<TeamArchivedDomainEvent>
{
    public ValueTask HandleAsync(TeamArchivedDomainEvent domainEvent, CancellationToken cancellationToken)
        => removeTeamMembershipsHandler.HandleAsync(
            new RemoveTeamMembershipsCommand(domainEvent.TeamId.Value),
            cancellationToken);
}
