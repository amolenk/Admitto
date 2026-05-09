using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using EmailSettingsEntity = Amolenk.Admitto.Core.Email.Domain.Entities.EmailSettings;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;

internal sealed class DeleteEmailSettingsHandler(IEmailWriteStore writeStore)
    : ICommandHandler<DeleteEmailSettingsCommand>
{
    public async ValueTask HandleAsync(DeleteEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await writeStore.EmailSettings
            .FirstOrDefaultAsync(
                s => s.Scope == command.Scope && s.ScopeId == command.ScopeId,
                cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<EmailSettingsEntity>(command.ScopeId));

        if (command.ExpectedVersion != settings.Version)
        {
            throw new BusinessRuleViolationException(
                CommonErrors.ConcurrencyConflict(command.ExpectedVersion, settings.Version));
        }

        writeStore.EmailSettings.Remove(settings);
    }
}
