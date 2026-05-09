using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.CreateEmailSettings;

internal sealed record CreateEmailSettingsCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string SmtpHost,
    int SmtpPort,
    string FromAddress,
    EmailAuthMode AuthMode,
    string? Username,
    string? Password) : Command;
