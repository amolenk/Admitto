using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;

public interface IPostgresExceptionMapping
{
    bool TryMapToError(PostgresException ex, out Error error);
}
