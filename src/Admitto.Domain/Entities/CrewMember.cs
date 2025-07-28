using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a member of the crew for an event.
/// </summary>
public class CrewMember : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];

    private CrewMember()
    {
    }

    private CrewMember(
        Guid id,
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;

        _additionalDetails = additionalDetails;

        AddDomainEvent(new CrewMemberAddedDomainEvent(TicketedEventId, Id));
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();

    public static CrewMember Create(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails)
    {
        return new CrewMember(
            Guid.NewGuid(),
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList());
    }
}