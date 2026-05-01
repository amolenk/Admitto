using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Cryptography;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Amolenk.Admitto.Module.Registrations.Application.Common.Cryptography;

internal sealed class EventSigningKeyProvider(
    IRegistrationsWriteStore writeStore,
    IMemoryCache memoryCache)
    : IEventSigningKeyProvider
{
    public async ValueTask<ReadOnlyMemory<byte>> GetKeyAsync(
        TicketedEventId eventId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"skey:{eventId.Value}";

        if (memoryCache.TryGetValue(cacheKey, out byte[]? cached) && cached is not null)
        {
            return cached;
        }

        var encodedKey = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.Id == eventId)
            .Select(e => e.SigningKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (encodedKey is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        var keyBytes = Convert.FromBase64String(encodedKey);
        memoryCache.Set(cacheKey, keyBytes);

        return keyBytes;
    }

    internal static class Errors
    {
        public static readonly Error EventNotFound = new(
            "ticketed_event.not_found",
            "The ticketed event could not be found.",
            Type: ErrorType.NotFound);
    }
}
