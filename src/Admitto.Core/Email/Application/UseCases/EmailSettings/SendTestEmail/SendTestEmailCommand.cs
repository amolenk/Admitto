using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Recipient) : Command;
