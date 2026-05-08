using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Recipient) : Command;
