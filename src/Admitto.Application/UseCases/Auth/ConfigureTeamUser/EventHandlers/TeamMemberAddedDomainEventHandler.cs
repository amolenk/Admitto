using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser.EventHandlers;

public class TeamMemberAddedDomainEventHandler(ConfigureTeamUserHandler configureTeamUserHandler)
    : IEventualDomainEventHandler<TeamMemberAddedDomainEvent>
{
    public ValueTask HandleAsync(TeamMemberAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var member = domainEvent.Member;
        var command = new ConfigureTeamUserCommand(domainEvent.TeamId, member.Email, member.Role);

        return configureTeamUserHandler.HandleAsync(command, cancellationToken);
    }
}