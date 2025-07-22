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
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        DateTimeOffset registrationStartTime,
        DateTimeOffset registrationEndTime)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        RegistrationStartTime = registrationStartTime;
        RegistrationEndTime = registrationEndTime;

        AddDomainEvent(new TicketedEventCreatedDomainEvent(teamId, slug));
    }

    public Guid TeamId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public DateTimeOffset RegistrationStartTime { get; private set; }
    public DateTimeOffset RegistrationEndTime { get; private set; }
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static TicketedEvent Create(
        Guid teamId,
        string slug,
        string name,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        DateTimeOffset registrationStartTime,
        DateTimeOffset registrationEndTime)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.NameIsRequired);

        if (endTime < startTime)
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.EndTimeMustBeAfterStartTime);

        if (registrationStartTime >= registrationEndTime)
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.RegistrationEndTimeMustBeAfterRegistrationStartTime);

        if (registrationEndTime > startTime)
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.RegistrationMustCloseBeforeEvent);

        return new TicketedEvent(
            Guid.NewGuid(),
            teamId,
            slug,
            name,
            startTime,
            endTime,
            registrationStartTime,
            registrationEndTime);
    }

    public void AddTicketType(string slug, string name, string slotName, int maxCapacity)
    {
        if (_ticketTypes.Any(t => t.Slug == slug))
        {
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.TicketTypeAlreadyExists);
        }

        var ticketType = TicketType.Create(slug, name, slotName, maxCapacity);
        _ticketTypes.Add(ticketType);
    }

    public void SetEmailTemplate(EmailType type, EmailTemplate template)
    {
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

    public bool ReserveTickets(Guid attendeeId, IList<TicketSelection> tickets, bool ignoreAvailability = false)
    {
        if (tickets.Count == 0)
        {
            throw new BusinessRuleException(BusinessRuleError.TicketedEvent.TicketsAreRequired);
        }

        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.First(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            if (!ticketType.TryReserveTickets(ticketSelection.Quantity, ignoreAvailability))
            {
                AddDomainEvent(new TicketsUnavailableDomainEvent(attendeeId));
                return false;
            }
        }

        AddDomainEvent(new TicketsReservedDomainEvent(attendeeId));
        return true;
    }
}