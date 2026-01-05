using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a member in a team.
/// </summary>
public class TeamMember : Entity
{
    [JsonConstructor]
    private TeamMember(Guid id, TeamMemberRole role) : base(id)
    {
        Id = id;
        Role = role;
    }
    
    public TeamMemberRole Role { get; private set; }

    public static TeamMember Create(Guid id, TeamMemberRole role)
    {
        return new TeamMember(id, role);
    }
}
