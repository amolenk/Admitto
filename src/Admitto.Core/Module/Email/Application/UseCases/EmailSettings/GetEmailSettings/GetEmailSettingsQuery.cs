using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings;

internal sealed record GetEmailSettingsQuery(EmailSettingsScope Scope, Guid ScopeId) : Query<EmailSettingsDto?>;
