using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Composing;

/// <summary>
/// Cause-specific facts for one transactional email. Recipient selection,
/// idempotency, claims, and delivery are deliberately outside this model.
/// </summary>
internal abstract record TransactionalEmailIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId);

internal sealed record TicketConfirmationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    string FirstName,
    IReadOnlyList<string> TicketTypes)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal sealed record CouponInvitationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string CouponCode)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal sealed record WaitlistOfferIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string CouponCode,
    string TicketTypeName,
    DateTimeOffset ExpiresAt)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal abstract record RegistrationCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal sealed record AttendeeRequestCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : RegistrationCancellationIntent(TeamId, TicketedEventId, FirstName, RegistrationId);

internal sealed record ReconfirmAutoCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : RegistrationCancellationIntent(TeamId, TicketedEventId, FirstName, RegistrationId);

internal sealed record VisaLetterDeniedCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : RegistrationCancellationIntent(TeamId, TicketedEventId, FirstName, RegistrationId);

internal sealed record VerificationCodeIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string PlainCode)
    : TransactionalEmailIntent(TeamId, TicketedEventId);
