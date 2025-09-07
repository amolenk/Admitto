namespace Amolenk.Admitto.Application.Projections.ParticipantActivity;

public class ParticipantActivityView 
{
    public required Guid Id { get; init; }
    
    public required Guid TicketedEventId { get; init; }

    public required Guid ParticipantId { get; init; }
    
    public required Guid SourceId { get; init; }
    
    public required ParticipantActivity Activity { get; init; }
    
    public Guid? EmailLogId { get; init; }

    public required DateTimeOffset OccuredOn { get; init; }
}
