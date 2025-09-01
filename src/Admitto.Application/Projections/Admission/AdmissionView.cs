namespace Amolenk.Admitto.Application.Projections.Admission;

public class AdmissionView 
{
    public required Guid ParticipantId { get; init; }
    
    public required Guid TicketedEventId { get; init; }

    public required Guid PublicId { get; init; }

    public string Email { get; set; } = null!;
    
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string AttendeeStatus { get; set; } = null!;
    
    public Guid? AttendeeId { get; set; }
    
    public string ContributorStatus { get; set; } = null!;
    
    public Guid? ContributorId { get; set; }
    
    public DateTimeOffset LastModifiedAt { get; set; }
}
