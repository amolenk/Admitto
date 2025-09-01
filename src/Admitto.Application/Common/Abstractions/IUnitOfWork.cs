namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IUnitOfWork
{
    ApplicationRuleError? UniqueViolationError { get; set; }

    ValueTask SaveChangesAsync(
        Func<ValueTask>? onUniqueViolation = null,
        CancellationToken cancellationToken = default);
    
    void Clear();
}