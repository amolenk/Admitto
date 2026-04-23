using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.Settings;

/// <summary>
/// Email-module-internal contract for resolving effective SMTP settings for a given event,
/// falling back to team-scoped settings when no event-scoped settings exist.
/// </summary>
internal interface IEffectiveEmailSettingsResolver
{
    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);
}
