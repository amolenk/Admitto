using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;

public class UserBuilder
{
    public static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");
    
    private EmailAddress _emailAddress = DefaultEmail;
    private readonly List<(TeamId TeamId, TeamMembershipRole Role)> _memberships = [];
    
    public UserBuilder WithEmailAddress(EmailAddress emailAddress)
    {
        _emailAddress = emailAddress;
        return this;
    }

    public UserBuilder WithMembership(TeamId teamId, TeamMembershipRole role = TeamMembershipRole.Crew)
    {
        _memberships.Add((teamId, role));
        return this;
    }

    public User Build()
    {
        var user = User.Create(_emailAddress);

        foreach (var (teamId, role) in _memberships)
        {
            user.AddTeamMembership(teamId, role);
        }

        return user;
    }
}