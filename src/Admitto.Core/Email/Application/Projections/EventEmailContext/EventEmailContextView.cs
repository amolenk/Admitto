using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Abstractions;

namespace Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;

/// <summary>
/// Email-owned, eventually-consistent read model holding the slow-changing
/// team/event facts the Email module needs to render transactional and bulk
/// emails and to evaluate reconfirm policies hourly. One row per
/// <c>(TeamId, TicketedEventId)</c>, maintained by
/// <see cref="EventEmailContextProjector"/> from Organization and Registrations
/// integration events. Rows may be partial while complementary events are still
/// in flight; consumers validate required fields via
/// <see cref="HasRequiredRenderingContext"/> /
/// <see cref="HasCompleteReconfirmPolicy"/>.
/// </summary>
public sealed class EventEmailContextView : IIsVersioned
{
    // Required for EF Core
    private EventEmailContextView()
    {
    }

    private EventEmailContextView(TeamId teamId, TicketedEventId ticketedEventId, DateTimeOffset now)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        CreatedAt = now;
        LastUpdatedAt = now;
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId TicketedEventId { get; private set; }
    public string? EventName { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public string? PublicSlug { get; private set; }
    public string? TimeZone { get; private set; }
    public DateTimeOffset? ReconfirmOpensAt { get; private set; }
    public DateTimeOffset? ReconfirmClosesAt { get; private set; }
    public int? ReconfirmMinEmailIntervalHours { get; private set; }
    public TimeOnly? ReconfirmQuietHoursStart { get; private set; }
    public TimeOnly? ReconfirmQuietHoursEnd { get; private set; }
    public int? SelfServiceTicketTypeCount { get; private set; }
    public bool IsArchived { get; private set; }
    public uint TicketedEventVersion { get; private set; }
    public uint TicketCatalogVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public uint Version { get; set; }

    public static EventEmailContextView CreatePartial(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        DateTimeOffset now) => new(teamId, ticketedEventId, now);

    public bool UpdateEventContext(
        uint ticketedEventVersion,
        string eventName,
        string websiteUrl,
        string publicSlug,
        string timeZone,
        int selfServiceTicketTypeCount,
        TicketedEventReconfirmPolicySnapshot? reconfirmPolicy,
        bool isArchived,
        DateTimeOffset now)
    {
        if (!ApplyTicketedEventVersion(ticketedEventVersion))
            return false;

        EventName = eventName;
        WebsiteUrl = websiteUrl;
        PublicSlug = publicSlug;
        TimeZone = timeZone;
        SelfServiceTicketTypeCount = selfServiceTicketTypeCount;
        IsArchived = isArchived;
        UpdateReconfirmPolicy(reconfirmPolicy, now);
        LastUpdatedAt = now;
        return true;
    }

    public bool UpdateDetails(
        uint ticketedEventVersion,
        string eventName,
        string websiteUrl,
        string publicSlug,
        string timeZone,
        DateTimeOffset now)
    {
        if (!ApplyTicketedEventVersion(ticketedEventVersion))
            return false;

        EventName = eventName;
        WebsiteUrl = websiteUrl;
        PublicSlug = publicSlug;
        TimeZone = timeZone;
        LastUpdatedAt = now;
        return true;
    }

    public bool UpdateReconfirmPolicy(uint ticketedEventVersion, TicketedEventReconfirmPolicySnapshot? policy, DateTimeOffset now)
    {
        if (!ApplyTicketedEventVersion(ticketedEventVersion))
            return false;

        UpdateReconfirmPolicy(policy, now);
        return true;
    }

    private void UpdateReconfirmPolicy(TicketedEventReconfirmPolicySnapshot? policy, DateTimeOffset now)
    {
        ReconfirmOpensAt = policy?.OpensAt;
        ReconfirmClosesAt = policy?.ClosesAt;
        ReconfirmMinEmailIntervalHours = policy?.MinEmailIntervalHours;
        ReconfirmQuietHoursStart = policy?.QuietHoursStart;
        ReconfirmQuietHoursEnd = policy?.QuietHoursEnd;
        LastUpdatedAt = now;
    }

    public bool UpdateSelfServiceTicketTypeCount(uint ticketCatalogVersion, int count, DateTimeOffset now)
    {
        if (ticketCatalogVersion < TicketCatalogVersion)
            return false;

        TicketCatalogVersion = ticketCatalogVersion;
        SelfServiceTicketTypeCount = count;
        LastUpdatedAt = now;
        return true;
    }

    public bool MarkArchived(uint ticketedEventVersion, DateTimeOffset now)
    {
        if (!ApplyTicketedEventVersion(ticketedEventVersion))
            return false;

        IsArchived = true;
        LastUpdatedAt = now;
        return true;
    }

    public bool HasRequiredRenderingContext =>
        !string.IsNullOrWhiteSpace(EventName)
        && !string.IsNullOrWhiteSpace(WebsiteUrl)
        && !string.IsNullOrWhiteSpace(PublicSlug)
        && SelfServiceTicketTypeCount.HasValue;

    private bool ApplyTicketedEventVersion(uint ticketedEventVersion)
    {
        if (ticketedEventVersion < TicketedEventVersion)
            return false;

        TicketedEventVersion = ticketedEventVersion;
        return true;
    }

    public bool HasCompleteReconfirmPolicy =>
        !IsArchived
        && !string.IsNullOrWhiteSpace(TimeZone)
        && ReconfirmOpensAt.HasValue
        && ReconfirmClosesAt.HasValue
        && ReconfirmMinEmailIntervalHours.HasValue
        && ReconfirmQuietHoursStart.HasValue == ReconfirmQuietHoursEnd.HasValue;
}
