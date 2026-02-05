namespace Amolenk.Admitto.Organization.Contracts;

public class TicketTypeDto
{
    public required Guid Id { get; init; }
    
    public required string AdminLabel { get; init; }
    
    public required string PublicTitle { get; init; }
    
    public required string[] TimeSlots { get; init; }
}