using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;

internal sealed record DeleteEmailSettingsCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    uint ExpectedVersion) : Command;
