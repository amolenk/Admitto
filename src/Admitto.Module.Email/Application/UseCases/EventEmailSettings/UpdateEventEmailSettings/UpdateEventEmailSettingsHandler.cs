using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpdateEventEmailSettings;

/// <summary>
/// Updates an existing <see cref="Domain.Entities.EventEmailSettings"/> aggregate with optimistic
/// concurrency on <c>Version</c>. When <see cref="UpdateEventEmailSettingsCommand.Password"/> is
/// <see langword="null"/> the previously stored encrypted password is preserved unchanged.
/// </summary>
internal sealed class UpdateEventEmailSettingsHandler(
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret)
    : ICommandHandler<UpdateEventEmailSettingsCommand>
{
    public async ValueTask HandleAsync(UpdateEventEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await writeStore.EventEmailSettings.GetAsync(
            TicketedEventId.From(command.TicketedEventId),
            command.ExpectedVersion,
            cancellationToken);

        var smtpHost = command.SmtpHost is not null ? Hostname.From(command.SmtpHost) : (Hostname?)null;
        var smtpPort = command.SmtpPort.HasValue ? Port.From(command.SmtpPort.Value) : (Port?)null;
        var fromAddress = command.FromAddress is not null
            ? EmailAddress.From(command.FromAddress)
            : (EmailAddress?)null;
        var username = command.Username is not null ? SmtpUsername.From(command.Username) : (SmtpUsername?)null;

        var protectedPassword = command.Password is not null
            ? ProtectedPassword.FromCiphertext(protectedSecret.Protect(command.Password))
            : (ProtectedPassword?)null;

        settings.Update(
            smtpHost,
            smtpPort,
            fromAddress,
            command.AuthMode,
            username,
            protectedPassword);
    }
}
