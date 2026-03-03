using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Shared.Infrastructure.Persistence;

public interface IPostgresExceptionMapping
{
    bool TryMapToError(PostgresException ex, out Error error);
}
