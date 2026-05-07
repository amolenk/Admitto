using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed record DeleteEmailTemplateCommand(
    EmailTemplateId Id,
    uint ExpectedVersion) : Command;
