using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed record DeleteEmailTemplateCommand(
    Guid Id,
    uint ExpectedVersion) : Command;
