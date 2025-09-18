namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IUnitOfWork
{
    Action<UniqueViolationArgs>? OnUniqueViolation { get; set; }
    
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
    
    void Clear();
}

public class UniqueViolationArgs
{
    public required ApplicationRuleError Error { get; set; }
    public bool Retry { get; set; }
};