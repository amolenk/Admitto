namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;

/// <summary>
/// Thrown when the Email-owned event context projection is missing or does not
/// yet carry the required rendering fields for a team/event. Surfaces a
/// deterministic failure when a rendering read races ahead of the projection
/// updates feeding it.
/// </summary>
internal sealed class EventEmailContextMissingException(Guid teamId, Guid ticketedEventId)
    : InvalidOperationException($"Email event context is missing required rendering fields for team '{teamId}' event '{ticketedEventId}'.");
