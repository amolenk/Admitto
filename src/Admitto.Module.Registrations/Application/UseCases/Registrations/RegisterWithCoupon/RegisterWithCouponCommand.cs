using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterWithCoupon;

internal sealed record RegisterWithCouponCommand(
    TicketedEventId EventId,
    string CouponCode,
    EmailAddress Email,
    string[] TicketTypeSlugs) : Command<RegistrationId>;
