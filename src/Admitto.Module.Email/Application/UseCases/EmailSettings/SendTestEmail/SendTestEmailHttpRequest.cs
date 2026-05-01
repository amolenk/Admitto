using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

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
            EmailAddress.From(Recipient));
    }
}