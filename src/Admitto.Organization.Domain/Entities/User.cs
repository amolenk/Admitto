using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User : Aggregate<UserId>
{
    private readonly List<TeamMembership> _memberships;

    private User(
        UserId id,
        EmailAddress email)
        : base(id)
    {
        EmailAddress = email;
        
        _memberships = [];
    }

    public EmailAddress EmailAddress { get; private set; }
    
    public IReadOnlyCollection<TeamMembership> Memberships => _memberships;

    public static User Create(
        EmailAddress email)
    {
        var user = new User(
            UserId.New(),
            email);

        
        // TODO Add domain event

        return user;
    }

    public void AddTeamMembership(TeamId teamId, TeamMembershipRole role)
    {
        var teamMembership = _memberships.FirstOrDefault(m => m.Id == teamId);
        if (teamMembership is null)
        {
            _memberships.Add(TeamMembership.Create(teamId, role));
            return;
        }

        if (teamMembership.Role != role)
        {
            throw new BusinessRuleViolationException(Errors.UserAlreadyTeamMember(Id, teamId));
        }
    }
    
    private static class Errors
    {
        public static Error UserAlreadyTeamMember(UserId userId, TeamId teamId) =>
            new(
                "user.already_team_member",
                "The user already is a member of the team.",
                Details: new Dictionary<string, object?>
                {
                    ["userId"] = userId.Value,
                    ["teamId"] = teamId.Value
                });
    }
}