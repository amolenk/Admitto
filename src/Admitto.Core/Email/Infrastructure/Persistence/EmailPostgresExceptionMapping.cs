using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Npgsql;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence;

internal sealed class EmailPostgresExceptionMapping : IPostgresExceptionMapping
{
    public bool TryMapToError(PostgresException ex, out Error error)
    {
        if (ex.ConstraintName is "IX_email_settings_team" or "IX_email_settings_team_event")
        {
            error = AlreadyExistsError.Create<EmailSettings>();
            return true;
        }

        if (ex.ConstraintName is "IX_email_templates_team_name" or "IX_email_templates_team_event_name")
        {
            error = AlreadyExistsError.Create<Domain.Entities.EmailTemplate>();
            return true;
        }

        error = null!;
        return false;
    }
}
