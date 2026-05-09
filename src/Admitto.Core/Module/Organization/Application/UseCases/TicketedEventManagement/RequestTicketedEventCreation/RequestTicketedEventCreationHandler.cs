using Amolenk.Admitto.Core.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation;

internal sealed class RequestTicketedEventCreationHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RequestTicketedEventCreationCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        RequestTicketedEventCreationCommand command,
        CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams
            .FirstOrDefaultAsync(t => t.Id == TeamId.From(command.TeamId), cancellationToken)
            ?? throw new BusinessRuleViolationException(NotFoundError.Create<Team>(command.TeamId));

        var request = team.RequestEventCreation(
            DisplayName.From(command.Name),
            AbsoluteUrl.From(command.WebsiteUrl),
            AbsoluteUrl.From(command.BaseUrl),
            command.StartsAt,
            command.EndsAt,
            TimeZoneId.From(command.TimeZone),
            UserId.From(command.RequesterId),
            DateTimeOffset.UtcNow);

        return request.Id.Value;
    }
}
