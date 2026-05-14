using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Organization.Domain;

public class UserBuilder
{
    public static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");
    
    private EmailAddress _emailAddress = DefaultEmail;
    private readonly List<(TeamId TeamId, TeamMembershipRole Role)> _memberships = [];
    private bool _isAdmin;
    
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

    public UserBuilder WithIsAdmin()
    {
        _isAdmin = true;
        return this;
    }

    public User Build()
    {
        var user = _isAdmin
            ? User.CreateAdmin(_emailAddress)
            : User.Create(_emailAddress);

        foreach (var (teamId, role) in _memberships)
        {
            user.AddTeamMembership(teamId, role);
        }

        return user;
    }
}