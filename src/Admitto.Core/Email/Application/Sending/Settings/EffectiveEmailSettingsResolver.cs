namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

/// <summary>
/// Email-module-internal contract for resolving deployment SMTP settings.
/// </summary>
internal interface IEffectiveEmailSettingsResolver
{
    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);
}

internal sealed class EffectiveEmailSettingsResolver : IEffectiveEmailSettingsResolver
{
    private readonly ISystemEmailSettingsResolver _systemSettingsResolver;

    public EffectiveEmailSettingsResolver(ISystemEmailSettingsResolver systemSettingsResolver) =>
        _systemSettingsResolver = systemSettingsResolver;

    public ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(teamId, cancellationToken);
    }

    public ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_systemSettingsResolver.Resolve());
    }
}
