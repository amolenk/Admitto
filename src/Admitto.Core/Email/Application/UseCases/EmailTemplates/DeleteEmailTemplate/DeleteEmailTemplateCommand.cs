using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed record DeleteEmailTemplateCommand(
    Guid Id,
    uint ExpectedVersion) : Command;
