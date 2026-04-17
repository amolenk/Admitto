using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Entities;

/// <summary>
/// Represents an organizing team in the system.
/// </summary>
public class Team : Aggregate<TeamId>
{
    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private Team()
    {
    }
    
    private Team(
        TeamId id,
        Slug slug,
        DisplayName name,
        EmailAddress emailAddress,
        DateTimeOffset? archivedAt,
        int ticketedEventScopeVersion)
        : base(id)
    {
        Slug = slug;
        Name = name;
        EmailAddress = emailAddress;
        ArchivedAt = archivedAt;
        TicketedEventScopeVersion = ticketedEventScopeVersion;
    }

    public Slug Slug { get; private set; }
    public DisplayName Name { get; private set; }
    public EmailAddress EmailAddress { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    /// <summary>
    /// Monotonically-increasing counter incremented each time a ticketed event is created under
    /// this team.
    /// </summary>
    /// <remarks>
    /// This is not a count of currently-active events — it never decrements. Its sole purpose is
    /// to force a write to the team row whenever a ticketed event is registered, which advances
    /// the EF row-version concurrency token (<c>Version</c>). Any concurrent
    /// <c>ArchiveTeam</c> operation that checked for active events before this increment will
    /// then fail with a concurrency conflict, closing the TOCTOU window.
    /// Updated via <see cref="RegisterTicketedEventCreation"/>, which is called by
    /// <c>TicketedEventCreatedDomainEventHandler</c> in the same transaction.
    /// </remarks>
    public int TicketedEventScopeVersion { get; private set; }
    public bool IsArchived => ArchivedAt.HasValue;

    public static Team Create(
        Slug slug,
        DisplayName name,
        EmailAddress emailAddress) =>
        new(
            TeamId.New(),
            slug,
            name,
            emailAddress,
            archivedAt: null,
            ticketedEventScopeVersion: 0);

    public void ChangeName(DisplayName name)
    {
        EnsureNotArchived();
        Name = name;
    }
    
    public void ChangeEmailAddress(EmailAddress emailAddress)
    {
        EnsureNotArchived();
        EmailAddress = emailAddress;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (IsArchived)
        {
            throw new BusinessRuleViolationException(Errors.TeamAlreadyArchived(Id));
        }

        ArchivedAt = archivedAt;
    }

    /// <summary>
    /// Increments <see cref="TicketedEventScopeVersion"/> to signal that a new ticketed event
    /// was registered under this team.
    /// </summary>
    /// <remarks>
    /// Called by <c>TicketedEventCreatedDomainEventHandler</c> when processing a
    /// <c>TicketedEventCreatedDomainEvent</c>. The increment forces a write to the team row,
    /// advancing the EF row-version concurrency token so any racing <c>ArchiveTeam</c> operation
    /// sees a conflict. Also validates the team is not already archived.
    /// </remarks>
    public void RegisterTicketedEventCreation()
    {
        EnsureNotArchived();
        TicketedEventScopeVersion++;
    }

    public void EnsureNotArchived()
    {
        if (IsArchived)
        {
            throw new BusinessRuleViolationException(Errors.TeamArchived(Id));
        }
    }

    internal static class Errors
    {
        public static Error TeamArchived(TeamId teamId) =>
            new(
                "team.archived",
                "The team is archived.",
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId.Value
                });

        public static Error TeamAlreadyArchived(TeamId teamId) =>
            new(
                "team.already_archived",
                "The team is already archived.",
                Details: new Dictionary<string, object?>
                {
                    ["teamId"] = teamId.Value
                });
    }
}
