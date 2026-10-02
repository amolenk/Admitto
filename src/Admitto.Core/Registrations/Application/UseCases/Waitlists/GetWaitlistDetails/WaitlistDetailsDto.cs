namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails;

public sealed record WaitlistDetailsDto(
    IReadOnlyList<WaitlistEntryRow> ActiveEntries,
    IReadOnlyList<PendingNotificationRow> PendingNotifications,
    WaitlistStats Stats);

public sealed record WaitlistEntryRow(
    Guid EntryId,
    int Position,
    Guid RegistrationId,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset JoinedAt);

public sealed record PendingNotificationRow(
    Guid CouponId,
    string MaskedEmail,
    DateTimeOffset ExpiresAt);

public sealed record WaitlistStats(
    int TotalWaiting,
    int TotalPending,
    int SentToday);
