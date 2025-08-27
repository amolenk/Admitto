using Amolenk.Admitto.Domain.Contracts;

namespace Amolenk.Admitto.Application.Projections.ParticipantActivity;

public class ParticipantActivityView
{
    public required Guid TicketedEventId { get; init; }

    public required string Email { get; init; }
    
    public required Guid SourceId { get; init; }
    
    public required string Activity { get; init; }
    
    public required DateTimeOffset OccuredAt { get; init; }
}
