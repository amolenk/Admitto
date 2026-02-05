using Amolenk.Admitto.Organization.Application.Persistence;

namespace Amolenk.Admitto.Organization.Application.Tests.Builders;

public class TicketedEventRecordBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _teamId = Guid.Empty;
    private string _slug = "event-slug";
    private string _name = "Event Name";
    private string _website = "https://example.com";
    private DateTimeOffset _startsAt = DateTimeOffset.MinValue;
    private DateTimeOffset _endsAt = DateTimeOffset.MaxValue;
    private string _baseUrl = "https://tickets.example.com";
    private List<TicketTypeRecord> _ticketTypes = [];

    public TicketedEventRecordBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TicketedEventRecordBuilder WithTeamId(Guid teamId)
    {
        _teamId = teamId;
        return this;
    }

    public TicketedEventRecordBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public TicketedEventRecordBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TicketedEventRecordBuilder WithWebsite(string website)
    {
        _website = website;
        return this;
    }

    public TicketedEventRecordBuilder WithBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    public TicketedEventRecordBuilder WithStartsAt(DateTimeOffset startsAt)
    {
        _startsAt = startsAt;
        return this;
    }

    public TicketedEventRecordBuilder WithEndsAt(DateTimeOffset endsAt)
    {
        _endsAt = endsAt;
        return this;
    }

    public TicketedEventRecordBuilder WithTicketType(TicketTypeRecord ticketType)
    {
        _ticketTypes.Add(ticketType);
        return this;
    }

    public TicketedEventRecordBuilder WithTicketType(Action<TicketTypeRecordBuilder> configure)
    {
        var builder = new TicketTypeRecordBuilder();
        configure(builder);

        _ticketTypes.Add(builder.Build());

        return this;
    }

    public TicketedEventRecord Build() => new()
    {
        Id = _id,
        TeamId = _teamId,
        Slug = _slug,
        Name = _name,
        Website = _website,
        BaseUrl = _baseUrl,
        StartsAt = _startsAt,
        EndsAt = _endsAt,
        TicketTypes = _ticketTypes
    };
}