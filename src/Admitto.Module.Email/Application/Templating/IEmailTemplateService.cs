using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Loads an <see cref="EmailTemplate"/> for sending, using event-scoped → team-scoped → built-in default precedence.
/// </summary>
internal interface IEmailTemplateService
{
    ValueTask<EmailTemplate> LoadAsync(
        string type,
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the effective template for a team scope, using team-scoped → built-in default precedence.
    /// </summary>
    ValueTask<EmailTemplate> LoadAsync(
        string type,
        TeamId teamId,
        CancellationToken cancellationToken = default);
}
