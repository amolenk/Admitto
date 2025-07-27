using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amolenk.Admitto.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(IHttpContextAccessor contextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            
            entry.Entity.LastChangedAt = now;
            entry.Entity.LastChangedBy = contextAccessor?.HttpContext?.User.GetUserEmail() ?? "system";
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}