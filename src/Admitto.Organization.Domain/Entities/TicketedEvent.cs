using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : Aggregate<TicketedEventId>
{
    private readonly List<TicketType> _ticketTypes;

    private TicketedEvent(
        TicketedEventId id,
        TeamId teamId,
        Slug slug,
        TicketedEventName name,
        WebsiteUrl website,
        BaseUrl baseUrl,
        TimeWindow eventWindow,
        IReadOnlyList<TicketType> ticketTypes)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        Website = website;
        BaseUrl = baseUrl;
        EventWindow = eventWindow;

        _ticketTypes = ticketTypes.ToList();
    }

    public TeamId TeamId { get; private set; }
    public Slug Slug { get; private set; }
    public TicketedEventName Name { get; private set; }
    public WebsiteUrl Website { get; private set; }
    public BaseUrl BaseUrl { get; private set; }
    public TimeWindow EventWindow { get; private set; }
    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    // public CancellationPolicy CancellationPolicy { get; private set; } = null!;
    // public ReconfirmPolicy? ReconfirmPolicy { get; private set; }
    // public RegistrationPolicy? RegistrationPolicy { get; private set; } = null!;
    // public ReminderPolicy? ReminderPolicy { get; private set; }

    // public DateTimeOffset? RegistrationOpensAt => StartsAt - RegistrationPolicy?.OpensBeforeEvent;
    // public DateTimeOffset? RegistrationClosesAt => StartsAt - RegistrationPolicy?.ClosesBeforeEvent;

    // public IReadOnlyCollection<AdditionalDetailSchema> AdditionalDetailSchemas => _additionalDetailSchemas.AsReadOnly();

    
    public static TicketedEvent Create(
        TeamId teamId,
        Slug slug,
        TicketedEventName name,
        WebsiteUrl website,
        BaseUrl baseUrl,
        TimeWindow eventWindow) =>
        new(
            TicketedEventId.New(),
            teamId,
            slug,
            name,
            website,
            baseUrl,
            eventWindow,
            []);

    public static TicketedEvent Rehydrate(
        TicketedEventId id,
        TeamId teamId,
        Slug slug,
        TicketedEventName name,
        WebsiteUrl website,
        BaseUrl baseUrl,
        TimeWindow eventWindow,
        IReadOnlyList<TicketType> ticketTypes) =>
        new(
            id,
            teamId,
            slug,
            name,
            website,
            baseUrl,
            eventWindow,
            ticketTypes);

    public ValidationResult<TicketType> AddAdminGrantTicketType(
        string adminLabel,
        string publicTitle,
        IReadOnlyList<string>? timeSlots = null,
        int? capacity = null)
    {
        throw new NotImplementedException();
        
        // TODO Check adminLabel uniqueness?

        // timeSlots ??= [];
        //
        // var ticketType = new TicketType(
        //     TicketTypeId.New(),
        //     adminLabel,
        //     publicTitle,
        //     IsSelfService: false,
        //     IsSelfServiceAvailable: false,
        //     timeSlots?.ToArray() ?? [],
        //     capacity);
        //
        // _ticketTypes.Add(ticketType);
        //
        // return ticketType;
    }

    public ValidationResult<TicketType> AddSelfServiceTicketType(
        string adminLabel,
        string publicTitle,
        bool isAvailable,
        IReadOnlyList<string>? timeSlots = null,
        int? capacity = null)
    {
        throw new NotImplementedException();
        //
        // // TODO Check adminLabel uniqueness?
        //
        // timeSlots ??= [];
        //
        // var ticketType = new TicketType(
        //     TicketTypeId.New(),
        //     adminLabel,
        //     publicTitle,
        //     IsSelfService: true,
        //     IsSelfServiceAvailable: isAvailable,
        //     timeSlots?.ToArray() ?? [],
        //     capacity);
        //
        // _ticketTypes.Add(ticketType);
        //
        // return ticketType;
    }

    
    //
    // public void ClaimTickets(
    //     string email,
    //     DateTimeOffset registrationDateTime,
    //     IList<TicketSelection> tickets,
    //     IList<Coupon> coupons,
    //     bool ignoreCapacity = false)
    // {
    //     if (RegistrationPolicy is null
    //         || registrationDateTime < RegistrationOpensAt
    //         || registrationDateTime > RegistrationClosesAt)
    //     {
    //         throw new DomainRuleException(DomainRuleError.TicketedEvent.RegistrationClosed);
    //     }
    //
    //     if (tickets.Count == 0)
    //     {
    //         throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketsAreRequired);
    //     }
    //
    //     if (RegistrationPolicy.EmailDomainName is not null)
    //     {
    //         // TODO Implement email domain check
    //     }
    //
    //     // Check for slot overlaps across all selected tickets
    //     var allSelectedSlots = new List<string>();
    //     foreach (var ticketSelection in tickets)
    //     {
    //         var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
    //         if (ticketType is null)
    //         {
    //             throw new DomainRuleException(
    //                 DomainRuleError.TicketedEvent.InvalidTicketType(ticketSelection.TicketTypeSlug));
    //         }
    //
    //         // Add all slots for this ticket type (considering quantity)
    //         for (int i = 0; i < ticketSelection.Quantity; i++)
    //         {
    //             foreach (var slotName in ticketType.SlotNames)
    //             {
    //                 if (allSelectedSlots.Contains(slotName))
    //                 {
    //                     throw new DomainRuleException(DomainRuleError.TicketedEvent.OverlappingSlots());
    //                 }
    //
    //                 allSelectedSlots.Add(slotName);
    //             }
    //         }
    //     }
    //
    //     foreach (var ticketSelection in tickets)
    //     {
    //         var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
    //
    //         // Ensure that there's enough capacity for the requested tickets.
    //         // Ticket type validation already done above, so ticketType should not be null here
    //         if (!ticketType!.HasAvailableCapacity(ticketSelection.Quantity)
    //             && !ignoreCapacity
    //             && !coupons.Any(c => c.TicketTypeSlug == ticketType.Slug && c.Quantity >= ticketSelection.Quantity))
    //         {
    //             throw new DomainRuleException(DomainRuleError.TicketedEvent.CapacityExceeded(ticketType.Slug));
    //         }
    //
    //         ticketType.ClaimTickets(ticketSelection.Quantity);
    //     }
    // }
    //
    // public void ReleaseTickets(IEnumerable<TicketSelection> tickets)
    // {
    //     foreach (var ticketSelection in tickets)
    //     {
    //         var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
    //         ticketType!.ReleaseTickets(ticketSelection.Quantity);
    //     }
    // }
    //
    // public void UpdateDetails(
    //     string? name,
    //     string? website,
    //     string? baseUrl,
    //     DateTimeOffset? startsAt,
    //     DateTimeOffset? endsAt)
    // {
    //     if (name is not null)
    //     {
    //         if (string.IsNullOrWhiteSpace(name))
    //         {
    //             throw new DomainRuleException(DomainRuleError.TicketedEvent.NameIsRequired);
    //         }
    //
    //         Name = name;
    //     }
    //
    //     if (website is not null)
    //     {
    //         Website = website;
    //     }
    //
    //     if (baseUrl is not null)
    //     {
    //         BaseUrl = baseUrl;
    //     }
    //
    //     if (startsAt is null && endsAt is null) return;
    //
    //     if (startsAt is not null)
    //     {
    //         StartsAt = startsAt.Value;
    //     }
    //
    //     if (endsAt is not null)
    //     {
    //         EndsAt = endsAt.Value;
    //     }
    //
    //     if (endsAt < startsAt)
    //     {
    //         throw new DomainRuleException(DomainRuleError.TicketedEvent.EndTimeMustBeAfterStartTime);
    //     }
    // }
    //
    // public void UpdateMaxCapacity(string ticketTypeSlug, int maxCapacity)
    // {
    //     var ticketType = _ticketTypes.FirstOrDefault(t => t.Slug == ticketTypeSlug);
    //     if (ticketType is null)
    //     {
    //         throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketTypeNotFound(ticketTypeSlug));
    //     }
    //
    //     ticketType.UpdateMaxCapacity(maxCapacity);
    // }
    //
    // public void SetCancellationPolicy(CancellationPolicy policy)
    // {
    //     CancellationPolicy = policy;
    // }
    //
    // public void SetReconfirmPolicy(ReconfirmPolicy? policy)
    // {
    //     ReconfirmPolicy = policy;
    //
    //     AddDomainEvent(new ReconfirmPolicyUpdatedDomainEvent(TeamId, Id));
    // }
    //
    // public void SetRegistrationPolicy(RegistrationPolicy policy)
    // {
    //     RegistrationPolicy = policy;
    // }
    //
    // public void SetReminderPolicy(ReminderPolicy? policy)
    // {
    //     ReminderPolicy = policy;
    // }
    //
    // private static string GenerateSigningKey(int sizeInBytes = 32)
    // {
    //     var key = new byte[sizeInBytes]; // 32 bytes = 256-bit key
    //     RandomNumberGenerator.Fill(key);
    //     return Convert.ToBase64String(key);
    // }
}