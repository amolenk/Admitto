using Amolenk.Admitto.Domain.Contracts;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class Team : Aggregate
{
    private readonly List<TeamMember> _members = [];

    // EF Core constructor
    private Team()
    {
    }
    
    private Team(TeamId id, string slug, string name, string email, string emailServiceConnectionString) : base(id)
    {
        Slug = slug;
        Name = name;
        Email = email;
        EmailServiceConnectionString = emailServiceConnectionString;
        
        AddDomainEvent(new TeamCreatedDomainEvent(Id, slug));
    }

    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string EmailServiceConnectionString { get; private set; } = null!;
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();
    
    public static Team Create(string slug, string name, string email, string emailServiceConnectionString)
    {
        var id = TeamId.FromName(name);
        
        return new Team(id, slug, name, email, emailServiceConnectionString);
    }
    
    public void AddMember(string email, TeamMemberRole role)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainRuleException(DomainRuleError.Team.EmailIsRequired);
        
        var member = TeamMember.Create(email, role);

        if (_members.Any(m => m.Id == member.Id))
        {
            throw new DomainRuleException(DomainRuleError.Team.MemberAlreadyExists);
        }
        
        
        _members.Add(member);
        
        AddDomainEvent(new TeamMemberAddedDomainEvent(Slug, member));
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
