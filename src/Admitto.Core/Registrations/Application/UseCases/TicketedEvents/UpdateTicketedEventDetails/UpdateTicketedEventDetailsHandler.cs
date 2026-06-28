using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;

internal sealed class UpdateTicketedEventDetailsHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<UpdateTicketedEventDetailsCommand>
{
    public async ValueTask HandleAsync(
        UpdateTicketedEventDetailsCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        EventName name = EventName.From(command.Name);
        AbsoluteUrl websiteUrl = AbsoluteUrl.From(command.WebsiteUrl);
        AbsoluteUrl baseUrl = AbsoluteUrl.From(command.BaseUrl);
        TimeZoneId timeZone = TimeZoneId.From(command.TimeZone);
        Slug publicSlug = Slug.From(command.PublicSlug ?? command.Name);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        ticketedEvent.UpdateDetails(name, websiteUrl, baseUrl, publicSlug, timeZone, command.StartsAt, command.EndsAt);
        ticketedEvent.UpdateQuietHours(command.QuietHoursStart, command.QuietHoursEnd);
    }
}
