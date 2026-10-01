using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists;

/// <summary>
/// Looks up the <see cref="RegistrationId"/> behind a waitlist entry's email, so a waitlist-offer email can link
/// to the attendee's existing registration instead of a generic sign-up (ticket 01 gives every waitlist joiner a
/// registration, so this should always resolve). Returns <c>null</c> rather than throwing if it doesn't, so a
/// missed lookup degrades to the generic register link instead of blocking the offer.
/// </summary>
internal static class WaitlistRegistrationIdResolver
{
    /// <summary>
    /// Builds a resolver covering every email in <paramref name="emails"/>, via a single query.
    /// </summary>
    public static async ValueTask<Func<EmailAddress, RegistrationId?>> BuildAsync(
        IRegistrationsWriteStore writeStore,
        TicketedEventId eventId,
        TeamId teamId,
        IReadOnlyCollection<EmailAddress> emails,
        CancellationToken cancellationToken)
    {
        if (emails.Count == 0)
            return _ => null;

        var registrationIdsByEmail = await writeStore.Registrations
            .Where(r => r.EventId == eventId && r.TeamId == teamId && emails.Contains(r.Email))
            .ToDictionaryAsync(r => r.Email, r => r.Id, cancellationToken);

        return email => registrationIdsByEmail.TryGetValue(email, out var registrationId) ? registrationId : null;
    }
}
