using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

public interface IPostgresExceptionMapping
{
    bool TryMapToError(PostgresException ex, out Error error);
}
