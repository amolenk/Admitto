using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a contributor to an event (e.g. speaker, sponsor).
/// </summary>
public class Contributor : Aggregate
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<ContributorRole> _roles = [];

    // EF Core constructor
    private Contributor()
    {
    }

    private Contributor(
        Guid id,
        Guid ticketedEventId,
        Guid participantId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails,
        List<ContributorRole> roles)
        : base(id)
    {
        TicketedEventId = ticketedEventId;
        ParticipantId = participantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;

        _additionalDetails = additionalDetails;
        _roles = roles;

        AddDomainEvent(
            new ContributorAddedDomainEvent(ticketedEventId, participantId, id, email, firstName, lastName, roles));
    }

    public Guid TicketedEventId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();
    public IReadOnlyCollection<ContributorRole> Roles => _roles.AsReadOnly();

    public static Contributor Create(
        Guid ticketedEventId,
        Guid participantId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<ContributorRole> roles)
    {
        return new Contributor(
            Guid.NewGuid(),
            ticketedEventId,
            participantId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            roles.Distinct().ToList());
    }

    public void UpdateDetails(
        string? email,
        string? firstName,
        string? lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<ContributorRole> roles)
    {
        // TODO Specific events for email and name changes?
        
        if (email is not null)
        {
            Email = email;
        }

        if (firstName is not null)
        {
            FirstName = firstName;
        }

        if (lastName is not null)
        {
            LastName = lastName;
        }

        foreach (var detail in additionalDetails)
        {
            var existingDetail = _additionalDetails.FirstOrDefault(d => d.Name == detail.Name);
            if (existingDetail != null)
            {
                _additionalDetails.Remove(existingDetail);
            }

            _additionalDetails.Add(detail);
        }

        var roleList = roles.ToList();
        if (roleList.Count > 0)
        {
            var previousRoles = _roles.ToList();
            
            // Replace all roles if any are provided.
            _roles.Clear();
            _roles.AddRange(roleList.Distinct());

            AddDomainEvent(
                new ContributorRolesChangedDomainEvent(
                    TicketedEventId,
                    ParticipantId,
                    Id,
                    previousRoles,
                    _roles));
        }
    }

    public void MarkAsRemoved()
    {
        AddDomainEvent(new ContributorRemovedDomainEvent(TicketedEventId, ParticipantId, Id, Email, _roles));
    }
}