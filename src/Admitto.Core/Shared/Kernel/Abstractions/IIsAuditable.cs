namespace Amolenk.Admitto.Core.Shared.Kernel.Abstractions;

public interface IIsAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    
    DateTimeOffset LastChangedAt { get;set; }
    
    EmailAddress LastChangedBy { get; set; }
}