using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed class DeleteEmailTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<DeleteEmailTemplateCommand>
{
    public async ValueTask HandleAsync(DeleteEmailTemplateCommand command, CancellationToken cancellationToken)
    {
        EmailTemplateId id = EmailTemplateId.From(command.Id);
        var teamId = TeamId.From(command.TeamId);
        TicketedEventId? ticketedEventId = command.TicketedEventId.HasValue
            ? TicketedEventId.From(command.TicketedEventId.Value)
            : null;

        var template = await writeStore.EmailTemplates
            .FirstOrDefaultAsync(
                t => t.Id == id && t.TeamId == teamId && t.TicketedEventId == ticketedEventId,
                cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<EmailTemplate>());

        if (command.ExpectedVersion != template.Version)
        {
            throw new BusinessRuleViolationException(
                ConcurrencyConflictError.Create(command.ExpectedVersion, template.Version));
        }

        writeStore.EmailTemplates.Remove(template);
    }
}
