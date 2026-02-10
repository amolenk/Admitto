using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.Entities;

public class TeamMembership : Entity<TeamId>
{
    private TeamMembership(
        TeamId id,
        TeamMembershipRole role)
        : base(id)
    {
        Role = role;
    }

    public TeamMembershipRole Role { get; private set; }
    
    public static TeamMembership Create(
        TeamId teamId,
        TeamMembershipRole role) =>
        new(
            teamId,
            role);
}