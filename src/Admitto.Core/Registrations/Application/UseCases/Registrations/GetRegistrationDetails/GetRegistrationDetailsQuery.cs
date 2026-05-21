using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;

internal sealed record GetRegistrationDetailsQuery(
    Guid TeamId,
    TicketedEventId EventId,
    RegistrationId RegistrationId) : Query<RegistrationDetailDto?>;
