using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence;

// TODO Move to infrastructure
internal sealed class PostgresExceptionMapping : IPostgresExceptionMapping
{
    public bool TryMapToError(PostgresException ex, out Error error)
    {
        if (ex.ConstraintName == "IX_teams_slug")
        {
            error = AlreadyExistsError.Create<Team>();
            return true;
        }

        if (ex.ConstraintName == "IX_ticketed_events_team_id_slug")
        {
            error = AlreadyExistsError.Create<TicketedEvent>();
            return true;
        }

        error = null!;
        return false;
    }
}
