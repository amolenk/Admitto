using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Shared.Kernel.Abstractions;

public interface IIsAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    
    DateTimeOffset LastChangedAt { get;set; }
    
    EmailAddress LastChangedBy { get; set; }
}