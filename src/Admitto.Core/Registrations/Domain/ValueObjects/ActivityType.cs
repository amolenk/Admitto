namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

public enum ActivityType
{
    Registered = 0,
    Reconfirmed = 1,
    Cancelled = 2,
    TicketsChanged = 3,
    CheckedIn = 4,
    WaitlistOfferSent = 5,
    WaitlistOfferExpired = 6,
    WaitlistRemoved = 7,
    WaitlistSelectionChanged = 8
}
