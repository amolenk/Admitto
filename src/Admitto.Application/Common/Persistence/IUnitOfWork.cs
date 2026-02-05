namespace Amolenk.Admitto.Application.Common.Persistence;

public interface IUnitOfWork
{
    ValueTask<TResult> RunAsync<TResult>(Func<ValueTask<TResult>> operation, CancellationToken cancellationToken);
    
    Action<UniqueViolationArgs>? OnUniqueViolation { get; set; }
    
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
    
    void Clear();
}

public class UniqueViolationArgs
{
    public required ApplicationRuleError Error { get; set; }
    public bool Retry { get; set; }
};