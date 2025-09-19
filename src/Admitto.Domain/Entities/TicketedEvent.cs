using System.Security.Cryptography;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : Aggregate
{
    private readonly List<TicketType> _ticketTypes = [];
    private readonly List<AdditionalDetailSchema> _additionalDetailSchemas = [];

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
        string baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        List<AdditionalDetailSchema> additionalDetailSchemas)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        Website = website;
        StartsAt = startsAt;
        EndsAt = endsAt;
        BaseUrl = baseUrl;
        CancellationPolicy = CancellationPolicy.Default;
        RegistrationPolicy = RegistrationPolicy.Default; // TODO Remove
        SigningKey = GenerateSigningKey(32);

        _additionalDetailSchemas = additionalDetailSchemas;

        AddDomainEvent(new TicketedEventCreatedDomainEvent(teamId, Id, slug));
    }

    public Guid TeamId { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Website { get; private set; } = null!;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public CancellationPolicy CancellationPolicy { get; private set; } = null!;
    public ReconfirmPolicy? ReconfirmPolicy { get; private set; }
    public RegistrationPolicy? RegistrationPolicy { get; private set; } = null!;
    public ReminderPolicy? ReminderPolicy { get; private set; }
    public string SigningKey { get; private set; } = null!;
    
    public DateTimeOffset? RegistrationOpensAt => StartsAt - RegistrationPolicy?.OpensBeforeEvent;
    public DateTimeOffset? RegistrationClosesAt => StartsAt - RegistrationPolicy?.ClosesBeforeEvent;
    
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();
    public IReadOnlyCollection<AdditionalDetailSchema> AdditionalDetailSchemas => _additionalDetailSchemas.AsReadOnly();

    public static TicketedEvent Create(
        Guid teamId,
        string slug,
        string name,
        string website,
        string baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        IEnumerable<AdditionalDetailSchema> additionalDetailSchemas)
    {
        // TODO Additional validations

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException(DomainRuleError.TicketedEvent.NameIsRequired);

        if (endsAt < startsAt)
            throw new DomainRuleException(DomainRuleError.TicketedEvent.EndTimeMustBeAfterStartTime);

        return new TicketedEvent(
            Guid.NewGuid(),
            teamId,
            slug,
            name,
            website,
            baseUrl,
            startsAt,
            endsAt,
            additionalDetailSchemas.ToList());
    }

    public void AddTicketType(string slug, string name, string slotName, int maxCapacity)
    {
        AddTicketType(slug, name, new List<string> { slotName }, maxCapacity);
    }

    public void AddTicketType(string slug, string name, List<string> slotNames, int maxCapacity)
    {
        if (_ticketTypes.Any(t => t.Slug == slug))
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketTypeAlreadyExists);
        }

        var ticketType = TicketType.Create(slug, name, slotNames, maxCapacity);
        _ticketTypes.Add(ticketType);
    }

    public void ClaimTickets(
        string email,
        DateTimeOffset registrationDateTime,
        IList<TicketSelection> tickets,
        bool ignoreCapacity = false)
    {
        if (RegistrationPolicy is null 
            || registrationDateTime < RegistrationOpensAt 
            || registrationDateTime > RegistrationClosesAt)
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.RegistrationClosed);
        }
        
        if (tickets.Count == 0)
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketsAreRequired);
        }

        if (RegistrationPolicy.EmailDomainName is not null)
        {
            // TODO Implement email domain check
        }
        
        // Check for slot overlaps across all selected tickets
        var allSelectedSlots = new List<string>();
        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            if (ticketType is null)
            {
                throw new DomainRuleException(
                    DomainRuleError.TicketedEvent.InvalidTicketType(ticketSelection.TicketTypeSlug));
            }

            // Add all slots for this ticket type (considering quantity)
            for (int i = 0; i < ticketSelection.Quantity; i++)
            {
                foreach (var slotName in ticketType.SlotNames)
                {
                    if (allSelectedSlots.Contains(slotName))
                    {
                        throw new DomainRuleException(DomainRuleError.TicketedEvent.OverlappingSlots());
                    }
                    allSelectedSlots.Add(slotName);
                }
            }
        }

        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            // Ticket type validation already done above, so ticketType should not be null here

            // Ensure that there's enough capacity for the requested tickets.
            if (!ignoreCapacity && !ticketType!.HasAvailableCapacity(ticketSelection.Quantity))
            {
                throw new DomainRuleException(DomainRuleError.TicketedEvent.CapacityExceeded(ticketType.Slug));
            }

            ticketType!.ClaimTickets(ticketSelection.Quantity);
        }
    }

    public void ReleaseTickets(IList<TicketSelection> tickets)
    {
        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            ticketType!.ReleaseTickets(ticketSelection.Quantity);
        }
    }

    public void SetCancellationPolicy(CancellationPolicy policy)
    {
        CancellationPolicy = policy;
    }

    public void SetReconfirmPolicy(ReconfirmPolicy? policy)
    {
        ReconfirmPolicy = policy;
    }

    public void SetRegistrationPolicy(RegistrationPolicy policy)
    {
        RegistrationPolicy = policy;
    }

    public void SetReminderPolicy(ReminderPolicy? policy)
    {
        ReminderPolicy = policy;
    }
    
    private static string GenerateSigningKey(int sizeInBytes = 32)
    {
        var key = new byte[sizeInBytes]; // 32 bytes = 256-bit key
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}