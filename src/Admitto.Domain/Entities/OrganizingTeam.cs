using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class OrganizingTeam : AggregateRoot
{
    private readonly List<User> _members;

    // EF Core constructor
    private OrganizingTeam()
    {
        _members = [];
    }
    
    private OrganizingTeam(OrganizingTeamId id, string name) : base(id)
    {
        Id = id.Value;
        Name = name;
        _members = [];
    }

    public string Name { get; private set; } = null!;
    public IReadOnlyCollection<User> Members => _members.AsReadOnly();
    
    public static OrganizingTeam Create(string name)
    {
        var id = OrganizingTeamId.FromName(name);
        
        return new OrganizingTeam(id, name);
    }
    
    public void AddMember(User user)
    {
        // TODO Check if user is not already a member
        
        _members.Add(user);
        
        AddDomainEvent(new TeamMemberAddedDomainEvent(Id, user.Id, user.Email, user.Role));
    }
}
