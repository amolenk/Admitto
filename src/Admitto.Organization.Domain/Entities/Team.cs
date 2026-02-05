using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class Team : Aggregate<TeamId>
{
    private readonly List<TeamMember> _members = [];

    // EF Core constructor
    private Team()
    {
    }
    
    private Team(TeamId id, TeamSlug slug, string name, EmailAddress email) : base(id)
    {
        Slug = slug;
        Name = name;
        Email = email;
    }

    public TeamSlug Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public EmailAddress Email { get; private set; }
    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();
    
    public static Team Create(TeamSlug slug, string name, EmailAddress email)
    {
        var id = TeamId.New();
        
        return new Team(id, slug, name, email);
    }
    
    // public void AddMember(Guid userId, TeamMemberRole role)
    // {
    //     if (_members.Any(m => m.Id == userId))
    //     {
    //         throw new DomainRuleException(DomainRuleError.Team.MemberAlreadyExists);
    //     }
    //     
    //     var member = TeamMember.Create(userId, role);
    //
    //     _members.Add(member);
    //     
    //     AddDomainEvent(new TeamMemberAddedDomainEvent(Id, member));
    // }
    
    // public void UpdateDetails(string? requestName, string? requestEmail, string? requestEmailServiceConnectionString)
    // {
    //     if (!string.IsNullOrWhiteSpace(requestName))
    //     {
    //         Name = requestName;
    //     }
    //     
    //     if (!string.IsNullOrWhiteSpace(requestEmail))
    //     {
    //         Email = requestEmail;
    //     }
    //     
    //     if (!string.IsNullOrWhiteSpace(requestEmailServiceConnectionString))
    //     {
    //         EmailServiceConnectionString = requestEmailServiceConnectionString;
    //     }
    // }
}
