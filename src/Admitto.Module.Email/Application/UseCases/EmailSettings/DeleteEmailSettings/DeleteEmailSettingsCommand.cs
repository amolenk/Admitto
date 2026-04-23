using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;

internal sealed record DeleteEmailSettingsCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    uint ExpectedVersion) : Command;
