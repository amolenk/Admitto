using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence;

internal sealed class BadgesPostgresExceptionMapping : IPostgresExceptionMapping
{
    public bool TryMapToError(PostgresException ex, out Error error)
    {
        if (ex.ConstraintName == "IX_badge_types_event_id_name")
        {
            error = AlreadyExistsError.Create<BadgeType>();
            return true;
        }

        error = null!;
        return false;
    }
}
