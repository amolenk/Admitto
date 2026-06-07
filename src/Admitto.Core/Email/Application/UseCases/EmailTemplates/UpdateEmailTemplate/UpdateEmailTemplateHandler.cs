using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate;

internal sealed class UpdateEmailTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<UpdateEmailTemplateCommand>
{
    private static readonly Error CannotRenameBuiltIn = new(
        "email_template.cannot_rename_builtin",
        "Built-in templates cannot be renamed.");

    private static readonly Error ReservedNameError = new(
        "email_template.reserved_name",
        "This name is reserved for a built-in template and cannot be used for a custom template.");

    private static readonly Error AlreadyExistsError = new(
        "email_template.already_exists",
        "A template with this name already exists in this scope.");

    public async ValueTask HandleAsync(UpdateEmailTemplateCommand command, CancellationToken ct)
    {
        EmailTemplateId id = EmailTemplateId.From(command.Id);
        var teamId = TeamId.From(command.TeamId);
        TicketedEventId? ticketedEventId = command.TicketedEventId.HasValue
            ? TicketedEventId.From(command.TicketedEventId.Value)
            : null;

        var template = await writeStore.EmailTemplates.GetAsync(
            t => t.Id == id && t.TeamId == teamId && t.TicketedEventId == ticketedEventId,
            command.Version,
            ct);

        if (command.Version != template.Version)
            throw new BusinessRuleViolationException(
                ConcurrencyConflictError.Create(command.Version, template.Version));

        var nameChanged = command.Name is not null &&
            !string.Equals(template.Name, command.Name, StringComparison.OrdinalIgnoreCase);

        if (nameChanged)
        {
            if (BuiltInEmailTemplateNames.IsReserved(template.Name))
                throw new BusinessRuleViolationException(CannotRenameBuiltIn);

            if (BuiltInEmailTemplateNames.IsReserved(command.Name!))
                throw new BusinessRuleViolationException(ReservedNameError);

            var alreadyExists = await writeStore.EmailTemplates.AnyAsync(
                t => t.Id != id &&
                     t.TeamId == teamId &&
                     t.TicketedEventId == ticketedEventId &&
                     t.Name.ToLower() == command.Name!.ToLower(),
                ct);

            if (alreadyExists)
                throw new BusinessRuleViolationException(AlreadyExistsError);

            template.Rename(command.Name!);
        }

        template.Update(command.Subject, command.TextBody, command.HtmlBody);
    }
}
