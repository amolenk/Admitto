using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;

internal sealed record PreviewEmailTemplateQuery(
    TeamId TeamId,
    TicketedEventId? EventId,
    string Type) : Query<PreviewEmailTemplateDto>;
