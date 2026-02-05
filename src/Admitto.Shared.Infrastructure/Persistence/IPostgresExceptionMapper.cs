using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence;

public interface IPostgresExceptionMapper
{
    bool TryMap(PostgresException ex, out Error error);
}
