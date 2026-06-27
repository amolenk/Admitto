using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation;

internal sealed class RequestTicketedEventCreationHandler(IOrganizationWriteStore writeStore)
    : ICommandHandler<RequestTicketedEventCreationCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        RequestTicketedEventCreationCommand command,
        CancellationToken cancellationToken)
    {
        var team = await writeStore.Teams.GetAsync(
                 t => t.Id == TeamId.From(command.TeamId),
                 cancellationToken);

        var request = team.RequestEventCreation(
            EventName.From(command.Name),
            AbsoluteUrl.From(command.WebsiteUrl),
            AbsoluteUrl.From(command.BaseUrl),
            Slug.From(command.PublicSlug),
            command.StartsAt,
            command.EndsAt,
            TimeZoneId.From(command.TimeZone),
            UserId.From(command.RequesterId),
            DateTimeOffset.UtcNow);

        return request.Id.Value;
    }
}
