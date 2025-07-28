namespace Amolenk.Admitto.Domain.Entities;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    
    DateTimeOffset LastChangedAt { get;set; }
    
    string? LastChangedBy { get; set; }
}