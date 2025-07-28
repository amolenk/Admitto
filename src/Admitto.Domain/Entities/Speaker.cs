using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a speaker for an event.
/// </summary>
public class Speaker : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];

    private Speaker()
    {
    }

    private Speaker(
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

        AddDomainEvent(new SpeakerAddedDomainEvent(TicketedEventId, Id));
    }

    public Guid TeamId { get; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();

    public static Speaker Create(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails)
    {
        return new Speaker(
            Guid.NewGuid(),
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList());
    }
}