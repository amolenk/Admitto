using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Domain.Entities;

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

    public void ChangeRole(TeamMembershipRole newRole)
    {
        Role = newRole;
    }
}