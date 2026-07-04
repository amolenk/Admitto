using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;

namespace Amolenk.Admitto.Core.Email.Application.Persistence;

/// <summary>
/// Read-side persistence surface for the Email module's application
/// projections / read models. Mirrors the Registrations
/// <c>IRegistrationsReadStore</c> convention: each projection
/// <see cref="DbSet{TEntity}"/> lives on exactly one of the read/write store
/// interfaces. Projection maintainers (e.g.
/// <see cref="EventEmailContextProjector"/>) write through this read store; the
/// write store carries only the module's aggregates.
/// </summary>
public interface IEmailReadStore
{
    DbSet<EventEmailContextView> EventEmailContexts { get; }
    DbSet<TeamEmailContextView> TeamEmailContexts { get; }
}
