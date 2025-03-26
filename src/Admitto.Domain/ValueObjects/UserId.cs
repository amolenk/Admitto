using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a unique identifier for a user, based on the e-mail address.
/// </summary>
public record UserId(Guid Value)
{
    public static UserId FromEmail(string email)
    {
        return new UserId(DeterministicGuidGenerator.Generate(email));
    }
    
    public static implicit operator UserId(Guid value) => new(value);
    
    public static implicit operator Guid(UserId userId) => userId.Value;

}