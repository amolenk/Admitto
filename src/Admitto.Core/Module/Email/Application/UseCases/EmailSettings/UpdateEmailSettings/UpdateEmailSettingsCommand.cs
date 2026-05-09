using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;

internal sealed record UpdateEmailSettingsCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string? SmtpHost,
    int? SmtpPort,
    string? FromAddress,
    EmailAuthMode? AuthMode,
    string? Username,
    string? Password,
    uint ExpectedVersion) : Command;
