using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ResolvePartnerRegistration;

internal sealed record ResolvePartnerRegistrationQuery(
    Guid TeamId,
    TicketedEventId EventId,
    string Email) : Query<PartnerRegistrationResolutionDto?>;
