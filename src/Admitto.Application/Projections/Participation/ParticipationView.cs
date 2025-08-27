using Amolenk.Admitto.Domain.Contracts;

namespace Amolenk.Admitto.Application.Projections.Participation;

public class ParticipationView : IHasConcurrencyToken
{
    public required Guid TicketedEventId { get; init; }

    public required string Email { get; init; }
    
    public Guid? AttendeeRegistrationId { get; set; }

    public AttendeeStatus? AttendeeRegistrationStatus { get; set; }
    
    public uint? AttendeeRegistrationVersion { get; set; }

    public Guid? SpeakerEngagementId { get; set; }

    public uint? SpeakerEngagementVersion { get; set; }

    public Guid? CrewAssignmentId { get; set; }

    public uint? CrewAssignmentVersion { get; set; }
    
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