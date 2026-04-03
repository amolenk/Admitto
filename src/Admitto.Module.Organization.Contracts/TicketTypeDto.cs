namespace Amolenk.Admitto.Module.Organization.Contracts;

public class TicketTypeDto
{
    public required string Slug { get; init; }

    public required string Name { get; init; }

    public bool IsCancelled { get; init; }
}