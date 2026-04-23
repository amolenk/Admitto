using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.CreateEmailSettings;

internal sealed record CreateEmailSettingsCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password) : Command;
