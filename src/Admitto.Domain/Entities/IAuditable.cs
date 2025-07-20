namespace Amolenk.Admitto.Domain.Entities;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    
    DateTime LastChangedAt { get;set; }
    
    string? LastChangedBy { get; set; }
}