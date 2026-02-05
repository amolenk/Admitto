namespace Amolenk.Admitto.Organization.Application.Persistence;

public class TicketedEventRecord
{
    public Guid Id { get; init; }
    public Guid TeamId { get; init; }
    public string Slug { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Website { get; init; } = null!;
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    public string BaseUrl { get; init; } = null!;
    public List<TicketTypeRecord> TicketTypes { get; init; } = [];
}

public class TicketTypeRecord
{
    public Guid Id { get; init; }
    public string AdminLabel { get; init; } = null!;
    public string PublicTitle { get; init; } = null!;
    public bool IsSelfService { get; init; }
    public bool IsSelfServiceAvailable { get; init; }
    public List<string> TimeSlots { get; init; } = [];
    public int? Capacity { get; init; }
}
