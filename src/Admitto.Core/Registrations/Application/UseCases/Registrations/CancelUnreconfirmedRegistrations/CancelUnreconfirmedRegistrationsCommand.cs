using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelUnreconfirmedRegistrations;

internal sealed record CancelUnreconfirmedRegistrationsCommand(
    Guid TeamId,
    Guid TicketedEventId,
    IReadOnlyCollection<ReconfirmAutoExpiredRegistrationReference>? RegistrationReferences) : Command;
