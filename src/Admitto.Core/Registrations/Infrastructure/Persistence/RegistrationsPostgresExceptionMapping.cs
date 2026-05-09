using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;

internal sealed class RegistrationsPostgresExceptionMapping : IPostgresExceptionMapping
{
    public bool TryMapToError(PostgresException ex, out Error error)
    {
        if (ex.ConstraintName == "IX_registrations_event_id_email")
        {
            error = AlreadyExistsError.Create<Registration>();
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
