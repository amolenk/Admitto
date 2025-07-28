using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a team member, based on the email address.
/// </summary>
public record TeamMemberId(Guid Value)
{
    public static TeamMemberId FromEmail(string email)
    {
        return new TeamMemberId(DeterministicGuid.Create(email));
    }
    
    public static implicit operator TeamMemberId(Guid value) => new(value);
    
    public static implicit operator Guid(TeamMemberId userId) => userId.Value;
}