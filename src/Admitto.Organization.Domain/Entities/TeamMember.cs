using System.Text.Json.Serialization;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Organization.Domain.Entities;

/// <summary>
/// Represents a member in a team.
/// </summary>
public class TeamMember : Entity<UserId>
{
    [JsonConstructor]
    private TeamMember(UserId id, TeamMemberRole role) : base(id)
    {
        Id = id;
        Role = role;
    }
    
    public TeamMemberRole Role { get; private set; }

    public static TeamMember Create(TeamMemberId id, TeamMemberRole role)
    {
        throw new NotImplementedException();
        // return new TeamMember(id, role);
    }
}
