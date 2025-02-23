using System.ComponentModel.DataAnnotations;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a single attendee. An attendee can have zero or more registrations
/// for upcoming events.
/// </summary>
public class Attendee : AggregateRoot
{
    private readonly List<AttendeeRegistration> _registrations;

    private Attendee(Guid id, string email) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        
        Email = email;
        _registrations = [];
    }
    
    public string Email { get; private set; }
    public IReadOnlyCollection<AttendeeRegistration> Registrations => _registrations.AsReadOnly();
    
    public static Guid GetId(string email)
    {
        return DeterministicGuidGenerator.Generate(email);
    }
    
    public static Attendee Create(string email)
    {
        return new Attendee(GetId(email), email);
    }

    public AttendeeRegistration RegisterForEvent(Guid ticketedEventId, TicketOrder ticketOrder)
    {
        if (_registrations.Any(r => r.TicketedEventId == ticketedEventId))
            throw new ValidationException("Already registered for event.");

        var registration = AttendeeRegistration.Create(ticketedEventId, ticketOrder);
        _registrations.Add(registration);

        return registration;
    }

    public void FinalizeRegistration(Guid registrationId)
    {
        var registration = _registrations.FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
            throw new InvalidOperationException($"Registration '{registrationId}' does not exist.");

        registration.Finalize();
        
        AddDomainEvent(new RegistrationFinalizedDomainEvent(Id, registrationId));
    }

    public void RejectReservation(Guid registrationId)
    {
        var registration = _registrations.FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
            throw new InvalidOperationException($"Registration '{registrationId}' does not exist.");

        _registrations.Remove(registration);

        AddDomainEvent(new RegistrationRejectedDomainEvent(Id, registrationId));
    }

    public void CancelReservation()
    {
        // TODO Release tickets for event
    }

    public void CheckIn()
    {
        // TODO Change status.
        // TODO Remove reservation. BUT! What about idempotency? Maybe we need to scan twice!
    }

    public void MarkAsNoShow()
    {
        // TODO Change status.
        // TODO Remove reservation.
    }
}