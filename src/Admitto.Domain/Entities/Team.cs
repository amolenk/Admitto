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
    
    public void AddMember(Guid userId, TeamMemberRole role)
    {
        if (_members.Any(m => m.Id == userId))
        {
            throw new DomainRuleException(DomainRuleError.Team.MemberAlreadyExists);
        }
        
        var member = TeamMember.Create(userId, role);

        _members.Add(member);
        
        AddDomainEvent(new TeamMemberAddedDomainEvent(Id, member));
    }
    
    public void UpdateDetails(string? requestName, string? requestEmail, string? requestEmailServiceConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(requestName))
        {
            Name = requestName;
        }
        
        if (!string.IsNullOrWhiteSpace(requestEmail))
        {
            Email = requestEmail;
        }
        
        if (!string.IsNullOrWhiteSpace(requestEmailServiceConnectionString))
        {
            EmailServiceConnectionString = requestEmailServiceConnectionString;
        }
    }
}
