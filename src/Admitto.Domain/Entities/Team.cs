using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class Team : AggregateRoot
{
    private readonly List<TeamMember> _members = [];

    // EF Core constructor
    private Team()
    {
    }
    
    private Team(TeamId id, string name, EmailSettings emailSettings) : base(id)
    {
        Name = name;
        EmailSettings = emailSettings;
    }

    public string Name { get; private set; } = null!;
    public EmailSettings EmailSettings { get; private set; } = null!;
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    public static Team Create(string name, EmailSettings emailSettings)
    {
        var id = TeamId.FromName(name);
        
        return new Team(id, name, emailSettings);
    }
    
    public void AddMember(string email, TeamMemberRole role)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email cannot be empty.");
        
        var member = TeamMember.Create(email, role);

        if (_members.Any(m => m.Id == member.Id))
        {
            throw new DomainException("Member already exists.");
        }
        
        _members.Add(member);
        
        AddDomainEvent(new TeamMemberAddedDomainEvent(Id, member));
    }

    // public void UpdateMember(string email, TeamMemberRole role)
    // {
    //     if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email cannot be empty.");
    //
    //     var member = _members.FirstOrDefault(m => m.Email == email);
    //     
    //     var member = TeamMember.Create(email, role);
    //     
    //     // TODO Check if user is not already a member
    //     
    //     _members.Add(member);
    //     
    //     AddDomainEvent(new TeamMemberAddedDomainEvent(Id, user.Id, user.Email, user.Role));
    // }
}
