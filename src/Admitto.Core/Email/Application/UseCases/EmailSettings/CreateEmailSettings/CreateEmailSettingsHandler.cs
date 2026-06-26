using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.CreateEmailSettings;

/// <summary>
/// Creates the team-scoped <see cref="Domain.Entities.EmailSettings"/> aggregate.
/// </summary>
/// <remarks>
/// Uniqueness (one settings record per team) is enforced by the unique index on
/// <c>team_id</c>; <see cref="Infrastructure.Persistence.EmailPostgresExceptionMapping"/>
/// translates the resulting Postgres error into <see cref="Shared.Kernel.ErrorHandling.AlreadyExistsError"/> on commit.
/// </remarks>
internal sealed class CreateEmailSettingsHandler(
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret)
    : ICommandHandler<CreateEmailSettingsCommand>
{
    public ValueTask HandleAsync(CreateEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var protectedPassword = command.AuthMode == EmailAuthMode.Basic && command.Password is not null
            ? ProtectedPassword.FromCiphertext(protectedSecret.Protect(command.Password))
            : (ProtectedPassword?)null;

        var settings = Domain.Entities.EmailSettings.Create(
            TeamId.From(command.TeamId),
            Hostname.From(command.SmtpHost),
            Port.From(command.SmtpPort),
            EmailAddress.From(command.FromAddress),
            command.AuthMode,
            command.Username is not null ? SmtpUsername.From(command.Username) : (SmtpUsername?)null,
            protectedPassword,
            command.AccentColor is not null ? EmailAccentColor.From(command.AccentColor) : null,
            command.FontFamily is not null ? EmailFontFamily.From(command.FontFamily) : null);

        writeStore.EmailSettings.Add(settings);

        return ValueTask.CompletedTask;
    }
}
