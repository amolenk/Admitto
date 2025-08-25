using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : AggregateRoot
{
    private readonly List<TicketType> _ticketTypes = [];

    // EF Core constructor
    private TicketedEvent()
    {
    }

    private TicketedEvent(
        Guid id,
        Guid teamId,
        string slug,
        string name,
        string website,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        DateTimeOffset registrationStartTime,
        DateTimeOffset registrationEndTime,
        string baseUrl)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        Website = website;
        StartTime = startTime;
        EndTime = endTime;
        RegistrationStartTime = registrationStartTime;
        RegistrationEndTime = registrationEndTime;
        BaseUrl = baseUrl;

        AddDomainEvent(new TicketedEventCreatedDomainEvent(teamId, slug));
    }

    public Guid TeamId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Website { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public DateTimeOffset RegistrationStartTime { get; private set; }
    public DateTimeOffset RegistrationEndTime { get; private set; }
    public string BaseUrl { get; private set; }
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static TicketedEvent Create(
        Guid teamId,
        string slug,
        string name,
        string website,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        DateTimeOffset registrationStartTime,
        DateTimeOffset registrationEndTime,
        string baseUrl)
    {
        // TODO Additional validations

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException(DomainRuleError.TicketedEvent.NameIsRequired);

        if (endTime < startTime)
            throw new DomainRuleException(DomainRuleError.TicketedEvent.EndTimeMustBeAfterStartTime);

        if (registrationStartTime >= registrationEndTime)
            throw new DomainRuleException(
                DomainRuleError.TicketedEvent.RegistrationEndTimeMustBeAfterRegistrationStartTime);

        if (registrationEndTime > startTime)
            throw new DomainRuleException(DomainRuleError.TicketedEvent.RegistrationMustCloseBeforeEvent);

        return new TicketedEvent(
            Guid.NewGuid(),
            teamId,
            slug,
            name,
            website,
            startTime,
            endTime,
            registrationStartTime,
            registrationEndTime,
            baseUrl);
    }

    public void AddTicketType(string slug, string name, string slotName, int maxCapacity)
    {
        if (_ticketTypes.Any(t => t.Slug == slug))
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketTypeAlreadyExists);
        }

        var ticketType = TicketType.Create(slug, name, slotName, maxCapacity);
        _ticketTypes.Add(ticketType);
    }

    public bool HasAvailableCapacity(IEnumerable<TicketSelection> tickets)
    {
        return tickets.All(t =>
            _ticketTypes.Any(tt => tt.Slug == t.TicketTypeSlug && tt.HasAvailableCapacity(t.Quantity)));
    }

    [Obsolete]
    public bool HasAvailableCapacity(IDictionary<string, int> tickets)
    {
        return tickets.All(t =>
            _ticketTypes.Any(tt => tt.Slug == t.Key && tt.HasAvailableCapacity(t.Value)));
    }

    public Guid Register(
        string email,
        string firstName,
        string lastName,
        IList<AdditionalDetail> additionalDetails,
        IList<TicketSelection> tickets)
    {
        if (tickets.Count == 0)
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketsAreRequired);
        }

        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            if (ticketType is null)
            {
                throw new DomainRuleException(
                    DomainRuleError.TicketedEvent.InvalidTicketType(ticketSelection.TicketTypeSlug));
            }
            
            ticketType.AllocateTickets(ticketSelection.Quantity);
        }

        return AddAttendeeRegisteredDomainEvent(email, firstName, lastName, additionalDetails, tickets);
    }
    
    public Guid Invite(
        string email,
        string firstName,
        string lastName,
        IList<AdditionalDetail> additionalDetails,
        IList<TicketSelection> tickets)
    {
        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            if (ticketType is null)
            {
                throw new DomainRuleException(
                    DomainRuleError.TicketedEvent.InvalidTicketType(ticketSelection.TicketTypeSlug));
            }
            
            ticketType.AllocateTickets(ticketSelection.Quantity, ignoreAvailability: true);
        }
        
        return AddAttendeeRegisteredDomainEvent(email, firstName, lastName, additionalDetails, tickets);
    }
    
    private Guid AddAttendeeRegisteredDomainEvent(
        string email,
        string firstName,
        string lastName,
        IList<AdditionalDetail> additionalDetails,
        IList<TicketSelection> tickets)
    {
        var registrationId = Guid.NewGuid();
        
        AddDomainEvent(new AttendeeRegisteredDomainEvent(
            TeamId,
            Id,
            registrationId,
            email,
            firstName,
            lastName,
            additionalDetails,
            tickets));

        return registrationId;
    }
}