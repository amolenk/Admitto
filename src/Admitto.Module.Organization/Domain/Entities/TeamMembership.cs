using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

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