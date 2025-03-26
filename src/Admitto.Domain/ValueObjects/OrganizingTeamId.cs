using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for an organizing team, based on the team name.
/// </summary>
public record OrganizingTeamId(Guid Value)
{
    public static OrganizingTeamId FromName(string name)
    {
        return new OrganizingTeamId(DeterministicGuidGenerator.Generate(name));
    }
    
    public static implicit operator OrganizingTeamId(Guid value) => new(value);
    
    public static implicit operator Guid(OrganizingTeamId organizingTeamId) => organizingTeamId.Value;

}