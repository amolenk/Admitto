using Amolenk.Admitto.Domain.Contracts;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Projections.Participation;

public class ParticipationView : IHasConcurrencyToken
{
    public required Guid TicketedEventId { get; init; }

    public required Guid RegistrationId { get; set; }

    public required string Email { get; init; }
    
    public AttendeeStatus? AttendeeStatus { get; set; }
    
    public ContributorRole? ContributorRole { get; set; }
    
    public DateTimeOffset LastModifiedAt { get; set; }

    public uint Version { get; set; }
}

public enum AttendeeStatus
{
    Registered,
    Reconfirmed,
    CheckedIn,
    Canceled,
    CanceledLate,
    NoShow
}