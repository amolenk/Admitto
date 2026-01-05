using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Projections.TeamMember;

public class TeamMemberView 
{
    public required Guid UserId { get; init; }
    
    public required Guid TeamId { get; init; }
    
    public required TeamMemberRole Role { get; init; }
    
    public required DateTimeOffset AssignedAt { get; init; }
}
