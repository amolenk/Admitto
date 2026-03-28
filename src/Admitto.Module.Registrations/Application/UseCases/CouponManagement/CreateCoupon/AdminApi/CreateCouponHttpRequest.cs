using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.CreateCoupon.AdminApi;

public sealed record CreateCouponHttpRequest(
    string Email,
    Guid[] AllowedTicketTypeIds,
    DateTimeOffset ExpiresAt,
    bool BypassRegistrationWindow = false)
{
    internal CreateCouponCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        EmailAddress.From(Email),
        AllowedTicketTypeIds.Select(TicketTypeId.From).ToArray(),
        ExpiresAt,
        BypassRegistrationWindow);
}
