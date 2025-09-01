namespace Amolenk.Admitto.Application.Projections.ParticipantHistory;

public class ParticipantHistoryView 
{
    public required Guid Id { get; init; }
    
    public required Guid TicketedEventId { get; init; }

    public required Guid ParticipantId { get; init; }
    
    public required Guid SourceId { get; init; }
    
    public required string Activity { get; init; }

    public string? EmailType { get; init; }
    
    public Guid? EmailLogId { get; init; }

    public required DateTimeOffset OccuredAt { get; init; }
}
