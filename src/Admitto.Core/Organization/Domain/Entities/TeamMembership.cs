using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Domain.Entities;

public class TeamMembership
{
    private TeamMembership(
        TeamId teamId,
        TeamMembershipRole role)
    {
        TeamId = teamId;
        Role = role;
    }

    public TeamId TeamId { get; private set; }

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
