using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

// NOTE: EventStatus gating is reintroduced on TicketCatalog in section 7/9 of the
// redesign-ticketed-event-ownership change once the new TicketedEvent aggregate owns
// lifecycle transitions.
internal sealed class CreateCouponHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<CreateCouponCommand, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);
        EmailAddress email = EmailAddress.From(command.Email);

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(tc => tc.Id == eventId, cancellationToken);

        var availableTicketTypes = catalog?.TicketTypes
            .Select(tt => new TicketTypeInfo(tt.Id))
            .ToList() ?? [];

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

