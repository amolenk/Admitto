using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence;

internal sealed class RegistrationsPostgresExceptionMapping : IPostgresExceptionMapping
{
    public bool TryMapToError(PostgresException ex, out Error error)
    {
        if (ex.ConstraintName == "IX_registrations_event_id_email")
        {
            error = AlreadyExistsError.Create<Registration>();
            return true;
        }

        error = null!;
        return false;
    }
}
