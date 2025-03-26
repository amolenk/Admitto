using Amolenk.Admitto.Application.ReadModel.Views;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.ReadModel.Projections;

public class TeamMembersProjectionHandler(IReadModelContext context) 
    : IImmediateDomainEventHandler<TeamMemberAddedDomainEvent>
{
    public ValueTask HandleAsync(TeamMemberAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var teamMembersView = new TeamMembersView
        {
            TeamId = domainEvent.TeamId,
            UserId = domainEvent.UserId,
            UserEmail = domainEvent.Email,
            Role = domainEvent.Role
        };

        context.TeamMembers.Add(teamMembersView);
        
        return ValueTask.CompletedTask;
    }
}