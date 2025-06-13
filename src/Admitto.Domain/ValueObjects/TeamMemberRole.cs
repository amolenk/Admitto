namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a role for a team member.
/// </summary>
public record TeamMemberRole
{
    public const string Manager = "manager";
    public const string Organizer = "organizer";
    
    public TeamMemberRole(string value)
    {
        if (value != Manager && value != Organizer)
        {
            throw new ArgumentException($"'{value}' is not a valid team member role.");
        }
        
        Value = value;
    }

    public string Value { get; } = null!;
    
    public static implicit operator TeamMemberRole(string value) => new(value);
    
    public static implicit operator string(TeamMemberRole role) => role.Value;
}