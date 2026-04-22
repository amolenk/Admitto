using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence;

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

        error = null!;
        return false;
    }
}
