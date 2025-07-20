using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTeam.EventHandlers;

public class TeamCreatedDomainEventHandler(RegisterTeamHandler registerTeamHandler)
    : IEventualDomainEventHandler<TeamCreatedDomainEvent>
{
    public ValueTask HandleAsync(TeamCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new RegisterTeamCommand(domainEvent.TeamSlug);

        return registerTeamHandler.HandleAsync(command, cancellationToken);
    }
}
