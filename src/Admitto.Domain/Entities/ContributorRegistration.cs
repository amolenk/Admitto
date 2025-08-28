using Amolenk.Admitto.Domain.Contracts;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a contributor to an event (e.g. speaker, sponsor).
/// </summary>
public class ContributorRegistration : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];

    private ContributorRegistration()
    {
    }

    private ContributorRegistration(
        Guid id,
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails,
        ContributorRole role)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;

        _additionalDetails = additionalDetails;

        AddDomainEvent(new ContributorRegisteredDomainEvent(teamId, ticketedEventId, id, email, role));
    }

    public Guid TeamId { get; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public ContributorRole Role { get; private set; }

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();

    public static ContributorRegistration Create(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        ContributorRole role)
    {
        return new ContributorRegistration(
            Guid.NewGuid(),
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            role);
    }
}