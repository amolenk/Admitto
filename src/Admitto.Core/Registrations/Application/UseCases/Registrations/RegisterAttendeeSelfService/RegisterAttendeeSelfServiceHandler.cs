using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService;

internal sealed class RegisterAttendeeSelfServiceHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterAttendeeSelfServiceCommand, RegisterAttendeeSelfServiceResult>
{
    public async ValueTask<RegisterAttendeeSelfServiceResult> HandleAsync(
        RegisterAttendeeSelfServiceCommand command,
        CancellationToken cancellationToken)
    {
        var eventId = TicketedEventId.From(command.EventId);
        var teamId = TeamId.From(command.TeamId);
        var email = EmailAddress.From(command.Email);
        var firstName = FirstName.From(command.FirstName);
        var lastName = LastName.From(command.LastName);

        var ticketedEvent = await writeStore.TicketedEvents
            .GetAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        var registerTicketTypeIds = command.RegisterTicketTypeIds.Select(TicketTypeId.From).ToList();
        var waitlistTicketTypeIds = command.WaitlistTicketTypeIds.Select(TicketTypeId.From).ToList();

        EnsureNoDuplicateRequestedActions(registerTicketTypeIds, waitlistTicketTypeIds);

        var now = timeProvider.GetUtcNow();

        AdditionalDetails additionalDetails = AdditionalDetails.Empty;
        if (registerTicketTypeIds.Count > 0)
        {
            additionalDetails = AdditionalDetails.Validate(
                command.AdditionalDetails,
                ticketedEvent.AdditionalDetailSchema);

            ticketedEvent.EnsureRegistrationOpen(now);
            ticketedEvent.EnsureEmailDomainAllowed(email);
        }

        var existingRegistration = await writeStore.Registrations
            .SingleOrDefaultAsync(
                r => r.EventId == eventId && r.TeamId == teamId && r.Email == email,
                cancellationToken);

        if (registerTicketTypeIds.Count > 0 && existingRegistration?.Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(AlreadyExistsError.Create<Registration>());

        var catalog = await writeStore.TicketCatalogs
            .GetAsync(tc => tc.Id == eventId && tc.TeamId == teamId, cancellationToken);

        catalog.EnsureEventActive();
        ValidateWaitlistRequests(catalog, waitlistTicketTypeIds);
        var tickets = catalog.Claim(registerTicketTypeIds, enforce: true);

        Registration? registration = null;
        if (registerTicketTypeIds.Count > 0 && existingRegistration is null)
        {
            registration = Registration.Create(
                ticketedEvent.TeamId,
                eventId,
                email,
                firstName,
                lastName,
                tickets,
                additionalDetails,
                now);
            await writeStore.Registrations.AddAsync(registration, cancellationToken);
        }
        else if (registerTicketTypeIds.Count > 0 && existingRegistration is not null)
        {
            registration = existingRegistration;
            registration.Reset(firstName, lastName, tickets, additionalDetails, now);
        }

        foreach (var waitlistTicketTypeId in waitlistTicketTypeIds)
        {
            var waitlist = await writeStore.Waitlists
                .Include(w => w.Entries)
                .FirstOrDefaultAsync(
                    w => w.Id == waitlistTicketTypeId && w.EventId == eventId && w.TeamId == teamId,
                    cancellationToken);

            if (waitlist is null)
            {
                waitlist = Waitlist.Create(eventId, waitlistTicketTypeId, teamId);
                await writeStore.Waitlists.AddAsync(waitlist, cancellationToken);
            }

            waitlist.AddEntry(email, now);
        }

        return new RegisterAttendeeSelfServiceResult(
            registration?.Id.Value,
            registerTicketTypeIds.Select(id => id.Value).ToArray(),
            waitlistTicketTypeIds.Select(id => id.Value).ToArray());
    }

    private static void EnsureNoDuplicateRequestedActions(
        IReadOnlyList<TicketTypeId> registerTicketTypeIds,
        IReadOnlyList<TicketTypeId> waitlistTicketTypeIds)
    {
        var duplicateIds = registerTicketTypeIds
            .Concat(waitlistTicketTypeIds)
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.Value)
            .ToArray();

        if (duplicateIds.Length > 0)
            throw new BusinessRuleViolationException(TicketCatalog.Errors.DuplicateTicketTypes(duplicateIds));
    }

    private static void ValidateWaitlistRequests(
        TicketCatalog catalog,
        IReadOnlyList<TicketTypeId> waitlistTicketTypeIds)
    {
        foreach (var ticketTypeId in waitlistTicketTypeIds)
        {
            var ticketType = catalog.GetTicketType(ticketTypeId);
            if (ticketType is null)
                throw new BusinessRuleViolationException(TicketCatalog.Errors.UnknownTicketTypes([ticketTypeId.Value]));

            if (!ticketType.SelfServiceEnabled)
                throw new BusinessRuleViolationException(TicketCatalog.Errors.TicketTypesNotSelfService([ticketTypeId.Value]));

            if (!ticketType.WaitlistEnabled)
                throw new BusinessRuleViolationException(Errors.WaitlistNotEnabled(ticketTypeId));

            if (!ticketType.WaitlistMode)
                throw new BusinessRuleViolationException(Errors.StaleTicketState(ticketTypeId));
        }
    }

    internal static class Errors
    {
        public static Error WaitlistNotEnabled(TicketTypeId id) => new(
            "registration.waitlist_not_enabled",
            "The waitlist is not enabled for this ticket type.",
            Type: ErrorType.Validation,
            Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error StaleTicketState(TicketTypeId id) => new(
            "registration.stale_ticket_state",
            "The requested ticket type state no longer matches the submitted action.",
            Type: ErrorType.Conflict,
            Details: new Dictionary<string, object?> { ["id"] = id.Value });
    }
}
