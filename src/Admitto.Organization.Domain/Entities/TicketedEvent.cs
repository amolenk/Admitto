using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEvent : Aggregate<TicketedEventId>
{
    private readonly List<TicketType> _ticketTypes = [];

    // ReSharper disable once UnusedMember.Local
    // Required for EF Core
    private TicketedEvent()
    {
    }

    private TicketedEvent(
        TicketedEventId id,
        TeamId teamId,
        Slug slug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        TimeWindow eventWindow,
        IReadOnlyList<TicketType> ticketTypes)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        EventWindow = eventWindow;

        _ticketTypes = ticketTypes.ToList();
    }

    public TeamId TeamId { get; private set; }
    public Slug Slug { get; private set; }
    public DisplayName Name { get; private set; }
    public AbsoluteUrl WebsiteUrl { get; private set; }
    public AbsoluteUrl BaseUrl { get; private set; }
    public TimeWindow EventWindow { get; private set; } = null!;
    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();
    
    public static TicketedEvent Create(
        TeamId teamId,
        Slug slug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        TimeWindow eventWindow) =>
        new(
            TicketedEventId.New(),
            teamId,
            slug,
            name,
            websiteUrl,
            baseUrl,
            eventWindow,
            []);
    
    // public ValidationResult<TicketType> AddAdminGrantTicketType(
    //     string adminLabel,
    //     string publicTitle,
    //     IReadOnlyList<string>? timeSlots = null,
    //     int? capacity = null)
    // {
    //     throw new NotImplementedException();
    //     
    //     // TODO Check adminLabel uniqueness?
    //
    //     // timeSlots ??= [];
    //     //
    //     // var ticketType = new TicketType(
    //     //     TicketTypeId.New(),
    //     //     adminLabel,
    //     //     publicTitle,
    //     //     IsSelfService: false,
    //     //     IsSelfServiceAvailable: false,
    //     //     timeSlots?.ToArray() ?? [],
    //     //     capacity);
    //     //
    //     // _ticketTypes.Add(ticketType);
    //     //
    //     // return ticketType;
    // }

    // public ValidationResult<TicketType> AddSelfServiceTicketType(
    //     string adminLabel,
    //     string publicTitle,
    //     bool isAvailable,
    //     IReadOnlyList<string>? timeSlots = null,
    //     int? capacity = null)
    // {
    //     throw new NotImplementedException();
    //     //
    //     // // TODO Check adminLabel uniqueness?
    //     //
    //     // timeSlots ??= [];
    //     //
    //     // var ticketType = new TicketType(
    //     //     TicketTypeId.New(),
    //     //     adminLabel,
    //     //     publicTitle,
    //     //     IsSelfService: true,
    //     //     IsSelfServiceAvailable: isAvailable,
    //     //     timeSlots?.ToArray() ?? [],
    //     //     capacity);
    //     //
    //     // _ticketTypes.Add(ticketType);
    //     //
    //     // return ticketType;
    // }
}