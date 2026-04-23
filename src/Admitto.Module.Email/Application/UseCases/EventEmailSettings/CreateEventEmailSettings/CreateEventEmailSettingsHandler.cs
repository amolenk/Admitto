using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.CreateEventEmailSettings;

/// <summary>
/// Creates the <see cref="Domain.Entities.EmailSettings"/> aggregate for an event.
/// </summary>
/// <remarks>
/// Uniqueness (one settings record per event) is enforced by the unique index on
/// <c>(Scope, ScopeId)</c>; <see cref="Infrastructure.Persistence.EmailPostgresExceptionMapping"/>
/// translates the resulting Postgres error into <see cref="AlreadyExistsError"/> on commit.
/// </remarks>
internal sealed class CreateEventEmailSettingsHandler(
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret)
    : ICommandHandler<CreateEventEmailSettingsCommand>
{
    public ValueTask HandleAsync(CreateEventEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var protectedPassword = command.AuthMode == EmailAuthMode.Basic && command.Password is not null
            ? ProtectedPassword.FromCiphertext(protectedSecret.Protect(command.Password))
            : (ProtectedPassword?)null;

        var settings = Domain.Entities.EmailSettings.Create(
            EmailSettingsScope.Event,
            command.TicketedEventId,
            Hostname.From(command.SmtpHost),
            Port.From(command.SmtpPort),
            EmailAddress.From(command.FromAddress),
            command.AuthMode,
            command.Username is not null ? SmtpUsername.From(command.Username) : (SmtpUsername?)null,
            protectedPassword);

        writeStore.EmailSettings.Add(settings);

        return ValueTask.CompletedTask;
    }
}
