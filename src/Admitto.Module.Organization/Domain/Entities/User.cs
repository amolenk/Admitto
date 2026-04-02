using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

/// <summary>
/// Represents a user in the system. Users can be members of multiple teams and can have different roles in each team.
/// They are linked to an external user ID, which is used for authentication and authorization purposes.
/// </summary>
public class User : Aggregate<UserId>
{
    private static readonly TimeSpan DeprovisionGracePeriod = TimeSpan.FromDays(7);

    private readonly List<TeamMembership> _memberships = [];

    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private User()
    {
    }

    private User(
        UserId id,
        EmailAddress email)
        : base(id)
    {
        EmailAddress = email;
    }

    public ExternalUserId? ExternalUserId { get; private set; }
    
    public EmailAddress EmailAddress { get; private set; }
    
    public IReadOnlyList<TeamMembership> Memberships => _memberships.AsReadOnly();

    /// <summary>
    /// When set, the user's IdP account is scheduled for deprovisioning after this point in time.
    /// Cleared when the user regains a team membership before the deadline.
    /// </summary>
    public DateTimeOffset? DeprovisionAfter { get; private set; }

    public static User Create(
        EmailAddress email)
    {
        var user = new User(
            UserId.New(),
            email);

        user.AddDomainEvent(new UserCreatedDomainEvent(user.Id, email));

        return user;
    }
    
    public void AssignExternalUserId(ExternalUserId externalUserId)
    {
        ExternalUserId = externalUserId;
    }

    public void AddTeamMembership(TeamId teamId, TeamMembershipRole role)
    {
        if (_memberships.Any(m => m.Id == teamId))
        {
            throw new BusinessRuleViolationException(Errors.UserAlreadyTeamMember(Id, teamId));
        }

        _memberships.Add(TeamMembership.Create(teamId, role));
        CancelDeprovisioning();
    }

    public void ChangeTeamMembershipRole(TeamId teamId, TeamMembershipRole newRole)
    {
        var membership = _memberships.FirstOrDefault(m => m.Id == teamId);
        if (membership is null)
        {
            throw new BusinessRuleViolationException(Errors.UserNotTeamMember(Id, teamId));
        }

        membership.ChangeRole(newRole);
    }

    public void RemoveTeamMembership(TeamId teamId)
    {
        var membership = _memberships.FirstOrDefault(m => m.Id == teamId);
        if (membership is null)
        {
            throw new BusinessRuleViolationException(Errors.UserNotTeamMember(Id, teamId));
        }

        _memberships.Remove(membership);

        if (_memberships.Count == 0)
        {
            DeprovisionAfter = DateTimeOffset.UtcNow.Add(DeprovisionGracePeriod);
        }
    }

    /// <summary>
    /// Cancels any pending IdP deprovisioning, e.g. when the user regains a team membership.
    /// </summary>
    public void CancelDeprovisioning()
    {
        DeprovisionAfter = null;
    }

    /// <summary>
    /// Clears the external user ID and cancels any pending deprovisioning after the IdP account
    /// has been successfully deleted.
    /// </summary>
    public void CompleteDeprovisioning()
    {
        ExternalUserId = null;
        DeprovisionAfter = null;
    }
    
    internal static class Errors
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

        public static Error UserNotTeamMember(UserId userId, TeamId teamId) =>
            new(
                "user.not_team_member",
                "The user is not a member of the team.",
                Details: new Dictionary<string, object?>
                {
                    ["userId"] = userId.Value,
                    ["teamId"] = teamId.Value
                });
    }
}