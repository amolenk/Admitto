using System.Security.Claims;
using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Amolenk.Admitto.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(ClaimsPrincipal? principal) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.UtcNow;

        // TODO Probably still doesn't work
        var username = principal?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "unknown";
        }

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            
            entry.Entity.LastChangedAt = now;
            entry.Entity.LastChangedBy = username;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}