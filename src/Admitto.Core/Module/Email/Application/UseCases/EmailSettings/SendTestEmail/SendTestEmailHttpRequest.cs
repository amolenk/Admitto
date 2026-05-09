using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailHttpRequest(
    string Recipient)
{
    public SendTestEmailCommand ToCommand(
        EmailSettingsScope scope,
        Guid scopeId)
    {
        return new SendTestEmailCommand(
            scope,
            scopeId,
            Recipient);
    }
}