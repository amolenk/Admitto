namespace Amolenk.Admitto.Domain.Contracts;

public interface IIsAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    
    DateTimeOffset LastChangedAt { get;set; }
}