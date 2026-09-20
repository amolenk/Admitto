using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.SharedScanner.ResolveSharedScannerSession;

internal sealed record ResolveSharedScannerSessionQuery(string Secret)
    : Query<SharedScannerSessionDto>;

/// <summary>
/// The minimal event context the anonymous shared scanner needs to bootstrap its
/// UI (early-arrival warning, time-zone-aware timestamps). Deliberately excludes
/// attendance summaries, dashboard navigation, or any other administration.
/// </summary>
public sealed record SharedScannerSessionDto(
    Guid TeamId,
    Guid EventId,
    string EventName,
    DateTimeOffset StartsAt,
    string TimeZone);
