using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a member in a team.
/// </summary>
public class TeamMember : Entity
{
    private TeamMember(TeamMemberId id, string email, TeamMemberRole role) : this(id.Value, email, role)
    {
    }

    [JsonConstructor]
    private TeamMember(Guid id, string email, TeamMemberRole role) : base(id)
    {
        Id = id;
        Email = email;
        Role = role;
    }
    
    public string Email { get; private set; }
    public TeamMemberRole Role { get; private set; }

    public static TeamMember Create(string email, TeamMemberRole role)
    {
        var id = TeamMemberId.FromEmail(email);
        
        return new TeamMember(id, email, role);
    }
}
