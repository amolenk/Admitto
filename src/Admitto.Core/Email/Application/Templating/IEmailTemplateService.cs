using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Templating;

/// <summary>
/// Loads an <see cref="EmailTemplate"/> for sending, using event-scoped → team-scoped → built-in default precedence.
/// </summary>
internal interface IEmailTemplateService
{
    ValueTask<EmailTemplate> LoadAsync(
        string name,
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the effective template for a team scope, using team-scoped → built-in default precedence.
    /// </summary>
    ValueTask<EmailTemplate> LoadAsync(
        string name,
        TeamId teamId,
        CancellationToken cancellationToken = default);
}
