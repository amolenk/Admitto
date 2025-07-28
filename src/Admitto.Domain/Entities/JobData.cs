namespace Amolenk.Admitto.Domain.Entities;

public record JobData
{
    public Guid JobId { get; set; } = Guid.NewGuid();
}