using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;

internal sealed record UpdatePartnerRegistrationCommand(
    Guid EventId,
    Guid TeamId,
    Guid RegistrationId,
    string FirstName,
    string LastName,
    IReadOnlyList<Guid> RegisterTicketTypeIds,
    IReadOnlyList<Guid> WaitlistTicketTypeIds,
    IReadOnlyDictionary<string, string>? AdditionalDetails = null,
    Guid? CouponCode = null) : Command;
