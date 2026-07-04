using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;

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
