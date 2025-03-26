using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

public enum UserRole
{
    GlobalAdmin = 0,
    TeamAdmin = 1,
    Organiser = 2
}

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : Entity
{
    // EF Core constructor
    private User() { }
    
    private User(UserId id, string email, UserRole role) : base(id)
    {
        Id = id.Value;
        Email = email;
        Role = role;
    }
    
    public string Email { get; private set; } = null!;
    public UserRole Role { get; private set; }

    public static User Create(string email, UserRole role)
    {
        var id = UserId.FromEmail(email);
        
        return new User(id, email, role);
    }
}
