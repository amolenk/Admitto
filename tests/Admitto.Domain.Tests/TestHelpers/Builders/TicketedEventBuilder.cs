using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Tests.TestHelpers.Builders;

public class TicketedEventBuilder
{
    private TeamId _teamId = Guid.NewGuid();
    private string _slug = "test-event";
    private string _name = "Test Event";
    private string _website = "https://example.com";
    private DateTime _startsAt = DateTime.UtcNow.AddDays(7);
    private DateTime _endsAt = DateTime.UtcNow.AddDays(8);
    private string _baseUrl = "https://example.com/tickets";
    private List<AdditionalDetailSchema> _additionalDetailSchemas = [new AdditionalDetailSchemaBuilder().Build()];

    public TicketedEventBuilder WithTeamId(TeamId teamId)
    {
        _teamId = teamId;
        return this;
    }

    public TicketedEventBuilder WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public TicketedEventBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TicketedEventBuilder WithWebsite(string website)
    {
        _website = website;
        return this;
    }

    public TicketedEventBuilder WithBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    public TicketedEventBuilder WithStartsAt(DateTime startDate)
    {
        _startsAt = startDate;
        return this;
    }

    public TicketedEventBuilder WithEndsAt(DateTime endDate)
    {
        _endsAt = endDate;
        return this;
    }

    public TicketedEventBuilder WithAdditionalDetailSchemas(List<AdditionalDetailSchema> additionalDetailSchemas)
    {
        _additionalDetailSchemas = additionalDetailSchemas;
        return this;
    }

    public TicketedEvent Build() => TicketedEvent.Create(
        _teamId,
        _slug,
        _name,
        _website,
        _baseUrl,
        _startsAt,
        _endsAt,
        _additionalDetailSchemas);
}