using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Coupons.CreateCoupon;

internal sealed class CreateCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCouponCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);

        var catalog = await writeStore.TicketCatalogs.GetUntrackedAsync(tc => tc.Id == eventId, cancellationToken);
        catalog.EnsureEventActive();

        var availableTicketTypes = catalog.TicketTypes
            .Select(tt => new TicketTypeInfo(tt.Id))
            .ToList();

        var coupon = Coupon.Create(
            eventId,
            teamId,
            email,
            command.AllowedTicketTypeIds.Select(TicketTypeId.From).ToList(),
            command.ExpiresAt,
            command.BypassRegistrationWindow,
            availableTicketTypes,
            timeProvider.GetUtcNow());

        await writeStore.Coupons.AddAsync(coupon, cancellationToken);

        return coupon.Id.Value;
    }
}
