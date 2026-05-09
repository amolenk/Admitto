using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Recipient) : Command;
