using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests;

/// <summary>
/// Simple IUnitOfWork adapter that delegates SaveChangesAsync to the underlying DbContext.
/// Used in tests to avoid the full DI infrastructure needed for keyed service injection.
/// </summary>
internal sealed class DbContextUnitOfWork(DbContext context) : IUnitOfWork
{
    public async ValueTask SaveChangesAsync(
        CancellationToken cancellationToken = default,
        bool retryConcurrencyConflicts = false)
        => await context.SaveChangesAsync(cancellationToken);
}
