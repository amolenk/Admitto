using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for an organizing team, based on the team name.
/// </summary>
public record TeamId(Guid Value)
{
    public static TeamId FromName(string name)
    {
        return new TeamId(DeterministicGuidGenerator.Generate(name));
    }
    
    public static implicit operator TeamId(Guid value) => new(value);
    
    public static implicit operator Guid(TeamId teamId) => teamId.Value;

}