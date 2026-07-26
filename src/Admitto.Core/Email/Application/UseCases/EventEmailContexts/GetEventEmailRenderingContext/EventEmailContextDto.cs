namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;

/// <summary>
/// Fully-derived team/event rendering context for a single email, assembled
/// from the Email-owned team/event context projections plus public-link
/// configuration. Returned by
/// <see cref="GetEventEmailRenderingContextHandler"/> and consumed by the
/// transactional <c>SendEmail</c> handlers and the bulk-email fan-out job.
/// </summary>
internal sealed record EventEmailContextDto(
    Guid TeamId,
    Guid TicketedEventId,
    string TeamName,
    string EventName,
    string WebsiteUrl,
    string PublicEventLink,
    string RegisterLink,
    string QRCodeLink,
    string CancelLink,
    string EditRegistrationLink,
    string TimeZone,
    DateTimeOffset? ReconfirmOpensAt,
    DateTimeOffset? ReconfirmClosesAt,
    int? ReconfirmCadenceHours,
    int? ReconfirmMinEmailIntervalHours,
    bool IsArchived);
