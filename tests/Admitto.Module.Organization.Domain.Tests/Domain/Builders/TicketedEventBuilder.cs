using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;

public class TicketedEventBuilder
{
    public static readonly Slug DefaultSlug = Slug.From("test-event");
    public static readonly DisplayName DefaultName = DisplayName.From("Test Event");
    public static readonly AbsoluteUrl DefaultWebsiteUrl = AbsoluteUrl.From("https://example.com");
    public static readonly AbsoluteUrl DefaultBaseUrl = AbsoluteUrl.From("https://example.com/events");
    public static readonly TimeWindow DefaultEventWindow = new(
        DateTimeOffset.UtcNow.AddDays(30),
        DateTimeOffset.UtcNow.AddDays(31));

    private TeamId _teamId = TeamId.New();
    private Slug _slug = DefaultSlug;
    private DisplayName _name = DefaultName;
    private AbsoluteUrl _websiteUrl = DefaultWebsiteUrl;
    private AbsoluteUrl _baseUrl = DefaultBaseUrl;
    private TimeWindow _eventWindow = DefaultEventWindow;
    private EventStatus _targetStatus = EventStatus.Active;

    public TicketedEventBuilder WithTeamId(TeamId teamId)
    {
        _teamId = teamId;
        return this;
    }

    public TicketedEventBuilder WithSlug(string slug)
    {
        _slug = Slug.From(slug);
        return this;
    }

    public TicketedEventBuilder WithName(string name)
    {
        _name = DisplayName.From(name);
        return this;
    }

    public TicketedEventBuilder WithStatus(EventStatus status)
    {
        _targetStatus = status;
        return this;
    }

    public TicketedEventBuilder AsActive() => WithStatus(EventStatus.Active);

    public TicketedEventBuilder AsCancelled() => WithStatus(EventStatus.Cancelled);

    public TicketedEventBuilder AsArchived() => WithStatus(EventStatus.Archived);

    public TicketedEvent Build()
    {
        var ticketedEvent = TicketedEvent.Create(_teamId, _slug, _name, _websiteUrl, _baseUrl, _eventWindow);

        if (_targetStatus == EventStatus.Cancelled)
        {
            ticketedEvent.Cancel();
        }
        else if (_targetStatus == EventStatus.Archived)
        {
            ticketedEvent.Archive();
        }

        return ticketedEvent;
    }
}
