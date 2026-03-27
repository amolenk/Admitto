using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Kernel.Abstractions;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(IUserContextAccessor userContextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;
        var emailAddress = EmailAddress.From(userContextAccessor.Current.EmailAddress);

        foreach (var entry in dbContext.ChangeTracker.Entries<IIsAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            
            entry.Entity.LastChangedAt = now;
            entry.Entity.LastChangedBy = emailAddress;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}