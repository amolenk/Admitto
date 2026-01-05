using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Projections.TeamMember;

public class TeamMemberViewProjector(IApplicationContext context)
    : ITransactionalDomainEventHandler<TeamMemberAddedDomainEvent>
{
    public ValueTask HandleAsync(TeamMemberAddedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var record = new TeamMemberView
        {
            UserId = domainEvent.Member.Id,
            TeamId = domainEvent.TeamId,
            Role = domainEvent.Member.Role,
            AssignedAt = domainEvent.OccurredOn
        };
        
        context.TeamMemberView.Add(record);
        
        return ValueTask.CompletedTask;
    }
}