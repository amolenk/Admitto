using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence;

internal sealed class EmailPostgresExceptionMapping : IPostgresExceptionMapping
{
    public bool TryMapToError(PostgresException ex, out Error error)
    {
        if (ex.ConstraintName == "PK_event_email_settings")
        {
            error = AlreadyExistsError.Create<EventEmailSettings>();
            return true;
        }

        error = null!;
        return false;
    }
}
