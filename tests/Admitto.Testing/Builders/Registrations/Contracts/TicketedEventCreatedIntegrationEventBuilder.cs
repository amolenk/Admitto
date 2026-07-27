using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

namespace Amolenk.Admitto.Testing.Builders.Registrations.Contracts;

/// <summary>
/// Builds <see cref="TicketedEventCreatedIntegrationEvent"/> instances for tests.
/// <para>
/// The contract itself only exposes the canonical constructor with the full field set, so that
/// production code cannot accidentally publish a partially-populated event. Tests that only care
/// about a couple of fields use this builder instead of a convenience constructor on the contract.
/// </para>
/// </summary>
public class TicketedEventCreatedIntegrationEventBuilder
{
    public const string DefaultName = "DevConf";
    public const string DefaultWebsiteUrl = "https://example.com";
    public const string DefaultPublicSlug = "devconf";
    public const string DefaultTimeZone = "UTC";

    private Guid _creationRequestId = Guid.NewGuid();
    private Guid _teamId = Guid.NewGuid();
    private Guid _ticketedEventId = Guid.NewGuid();
    private uint _ticketedEventVersion;
    private string _name = DefaultName;
    private string _websiteUrl = DefaultWebsiteUrl;
    private string _publicSlug = DefaultPublicSlug;
    private string _timeZone = DefaultTimeZone;
    private int _selfServiceTicketTypeCount = 1;
    private TicketedEventReconfirmPolicySnapshot? _reconfirmPolicy;
    private bool _isArchived;

    public TicketedEventCreatedIntegrationEventBuilder WithCreationRequestId(Guid creationRequestId)
    {
        _creationRequestId = creationRequestId;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithTeamId(Guid teamId)
    {
        _teamId = teamId;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithTicketedEventId(Guid ticketedEventId)
    {
        _ticketedEventId = ticketedEventId;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithTicketedEventVersion(uint ticketedEventVersion)
    {
        _ticketedEventVersion = ticketedEventVersion;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithWebsiteUrl(string websiteUrl)
    {
        _websiteUrl = websiteUrl;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithPublicSlug(string publicSlug)
    {
        _publicSlug = publicSlug;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithTimeZone(string timeZone)
    {
        _timeZone = timeZone;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithSelfServiceTicketTypeCount(int count)
    {
        _selfServiceTicketTypeCount = count;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder WithReconfirmPolicy(
        TicketedEventReconfirmPolicySnapshot? reconfirmPolicy)
    {
        _reconfirmPolicy = reconfirmPolicy;
        return this;
    }

    public TicketedEventCreatedIntegrationEventBuilder AsArchived()
    {
        _isArchived = true;
        return this;
    }

    public TicketedEventCreatedIntegrationEvent Build() =>
        new(
            _creationRequestId,
            _teamId,
            _ticketedEventId,
            _ticketedEventVersion,
            _name,
            _websiteUrl,
            _publicSlug,
            _timeZone,
            _selfServiceTicketTypeCount,
            _reconfirmPolicy,
            _isArchived);
}
