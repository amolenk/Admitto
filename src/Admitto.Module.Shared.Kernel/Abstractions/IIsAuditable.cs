using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Shared.Kernel.Abstractions;

public interface IIsAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    
    DateTimeOffset LastChangedAt { get;set; }
    
    EmailAddress LastChangedBy { get; set; }
}