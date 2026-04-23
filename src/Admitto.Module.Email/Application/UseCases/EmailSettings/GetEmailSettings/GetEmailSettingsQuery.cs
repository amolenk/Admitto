using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings;

internal sealed record GetEmailSettingsQuery(EmailSettingsScope Scope, Guid ScopeId) : Query<EmailSettingsDto?>;
