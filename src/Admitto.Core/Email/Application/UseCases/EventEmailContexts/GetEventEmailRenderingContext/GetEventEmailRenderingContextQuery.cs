using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;

/// <summary>
/// Resolves the fully-derived <see cref="EventEmailContextDto"/> for a single
/// email send from the Email-owned event context projection. When
/// <see cref="RegistrationId"/> is supplied, per-registration links
/// (cancel/QR/change-tickets) are materialised; otherwise the public event link
/// is used as a fallback. Throws
/// <see cref="EventEmailContextMissingException"/> when the projection is not
/// yet populated with the required rendering fields.
/// </summary>
internal sealed record GetEventEmailRenderingContextQuery(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId? RegistrationId) : Query<EventEmailContextDto>;
