using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;

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
