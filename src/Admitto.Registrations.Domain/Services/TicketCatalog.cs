using Amolenk.Admitto.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Registrations.Domain.Services;

public interface ITicketCatalog
{
    ValueTask<IReadOnlyDictionary<Slug, TicketTypeSnapshot>> GetAllAsync(
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);
}
