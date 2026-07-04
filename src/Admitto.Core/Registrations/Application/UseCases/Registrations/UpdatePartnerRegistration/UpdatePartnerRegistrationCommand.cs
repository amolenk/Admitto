using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;

internal sealed record UpdatePartnerRegistrationCommand(
    Guid EventId,
    Guid TeamId,
    Guid RegistrationId,
    string FirstName,
    string LastName,
    IReadOnlyList<Guid> TicketTypeIds,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null,
    Guid? WaitlistCouponCode = null) : Command;
