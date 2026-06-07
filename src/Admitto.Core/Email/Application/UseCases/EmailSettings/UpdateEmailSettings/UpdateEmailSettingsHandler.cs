using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;

/// <summary>
/// Updates an existing <see cref="EmailSettings"/> aggregate with optimistic concurrency on <c>Version</c>.
/// When <see cref="UpdateEmailSettingsCommand.Password"/> is <see langword="null"/> the previously stored
/// encrypted password is preserved unchanged.
/// </summary>
internal sealed class UpdateEmailSettingsHandler(
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret)
    : ICommandHandler<UpdateEmailSettingsCommand>
{
    public async ValueTask HandleAsync(UpdateEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await writeStore.EmailSettings.GetAsync(
             s => s.TeamId == TeamId.From(command.TeamId) &&
                  s.TicketedEventId == (command.TicketedEventId.HasValue ? TicketedEventId.From(command.TicketedEventId.Value) : null),
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

        settings.Update(smtpHost, smtpPort, fromAddress, command.AuthMode, username, protectedPassword);
    }
}
