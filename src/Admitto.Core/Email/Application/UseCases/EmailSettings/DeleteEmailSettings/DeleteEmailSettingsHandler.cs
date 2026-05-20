using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;

internal sealed class DeleteEmailSettingsHandler(IEmailWriteStore writeStore)
    : ICommandHandler<DeleteEmailSettingsCommand>
{
    public async ValueTask HandleAsync(DeleteEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await writeStore.EmailSettings.GetUntrackedAsync(
            s => s.Scope == command.Scope && s.ScopeId == command.ScopeId,
            command.ExpectedVersion,
            cancellationToken);

        writeStore.EmailSettings.Remove(settings);
    }
}
